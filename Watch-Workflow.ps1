<#
.SYNOPSIS
    Live view of every running Claude Code Workflow: which runs are in flight, which subagents are
    alive inside each, what each is doing right now, and what they finally returned.

.DESCRIPTION
    A workflow writes one transcript directory per run:

        <projects>\<project>\<session-id>\subagents\workflows\<run-id>\
            journal.jsonl                 one line per agent lifecycle event
            agent-<id>.jsonl              the full transcript of that subagent
            agent-<id>.meta.json          agent type / spawn depth

    By default this script DISCOVERS EVERY RUN under every project and every session, keeps the ones
    that are still active, and renders them all in one refreshing dashboard. Runs that finish drop
    out; the loop ends when nothing is active.

    It never writes anything and opens every file with FileShare::ReadWrite, so it is safe to run
    against workflows in flight.

    "Active" means the journal shows at least one agent started with no matching completed/failed
    event. That alone is not enough, because a killed workflow leaves its agents permanently
    "started" - so a run whose transcripts have not been touched for -StaleMinutes is reported as
    'stale' and does not keep the loop alive. Stale is a warning, not a success: an empty result
    from a stale run means the agents died, not that they found nothing.

.PARAMETER RunId
    Watch only this run, e.g. wf_bae7a900-0ac. Accepts several. Without it, every active run is
    watched.

.PARAMETER ProjectsRoot
    Where to search. Defaults to %USERPROFILE%\.claude\projects, i.e. all projects and all sessions.

.PARAMETER Root
    Legacy escape hatch: a single ...\subagents\workflows directory to look in, instead of searching.

.PARAMETER IncludeFinished
    Also show runs that have already finished. Off by default, so the dashboard is only what is live.

.PARAMETER StaleMinutes
    A run with no transcript write in this many minutes is reported 'stale' rather than 'running',
    and stops holding the watch loop open. Default 15.

.PARAMETER Last
    Show at most this many runs, most recently written first.

.PARAMETER IntervalSeconds
    Refresh period. Default 5.

.PARAMETER Once
    Render a single snapshot and exit, instead of looping.

.PARAMETER Detail
    Also print the tail of each agent's reasoning/tool stream, not just its last action.

.EXAMPLE
    .\Watch-Workflow.ps1
    .\Watch-Workflow.ps1 -IncludeFinished -Last 5
    .\Watch-Workflow.ps1 -RunId wf_bae7a900-0ac -IntervalSeconds 3
    .\Watch-Workflow.ps1 -Once -Detail
#>

[CmdletBinding()]
param(
    [string[]]$RunId,
    [string]$ProjectsRoot = "$env:USERPROFILE\.claude\projects",
    [string]$Root,
    [switch]$IncludeFinished,
    [int]$StaleMinutes = 15,
    [int]$Last = 0,
    [int]$IntervalSeconds = 5,
    [switch]$Once,
    [switch]$Detail
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ------------------------------------------------------------------ discovery

# A run directory is one that actually holds transcripts. Checking for the files rather than trusting
# the name keeps stray directories out, and costs one stat per candidate.
function Test-RunDirectory {
    param([System.IO.DirectoryInfo]$Dir)

    if (Test-Path -LiteralPath (Join-Path $Dir.FullName 'journal.jsonl')) { return $true }
    return [bool](Get-ChildItem -LiteralPath $Dir.FullName -Filter 'agent-*.jsonl' -EA SilentlyContinue |
        Select-Object -First 1)
}

function Find-RunDirectories {
    param([string]$ProjectsRoot, [string]$Root, [string[]]$RunId)

    $candidates = [System.Collections.Generic.List[object]]::new()

    if ($Root) {
        if (-not (Test-Path -LiteralPath $Root)) { throw "Workflow root not found: $Root" }
        Get-ChildItem -LiteralPath $Root -Directory -EA SilentlyContinue |
            ForEach-Object { $candidates.Add($_) }
    }
    else {
        if (-not (Test-Path -LiteralPath $ProjectsRoot)) { throw "Projects root not found: $ProjectsRoot" }

        # <projects>\<project>\<session>\subagents\workflows\<run-id>. Enumerated level by level
        # rather than with -Recurse: a recursive walk would descend into every session's whole
        # transcript tree, which is large and entirely irrelevant.
        foreach ($project in Get-ChildItem -LiteralPath $ProjectsRoot -Directory -EA SilentlyContinue) {
            foreach ($session in Get-ChildItem -LiteralPath $project.FullName -Directory -EA SilentlyContinue) {
                $workflows = Join-Path $session.FullName 'subagents\workflows'
                if (-not (Test-Path -LiteralPath $workflows)) { continue }

                Get-ChildItem -LiteralPath $workflows -Directory -EA SilentlyContinue |
                    ForEach-Object { $candidates.Add($_) }
            }
        }
    }

    $runs = $candidates | Where-Object { Test-RunDirectory $_ }

    if ($RunId) {
        $wanted = [System.Collections.Generic.HashSet[string]]::new(
            [string[]]$RunId, [System.StringComparer]::OrdinalIgnoreCase)
        $runs = $runs | Where-Object { $wanted.Contains($_.Name) }
    }

    return @($runs | Sort-Object LastWriteTime -Descending)
}

# ...\projects\<project>\<session>\subagents\workflows\<run> -> "<project> / <session-prefix>"
function Get-RunOrigin {
    param([System.IO.DirectoryInfo]$RunDir)

    $parts = $RunDir.FullName -split '[\\/]'
    $i = [array]::IndexOf($parts, 'projects')
    if ($i -lt 0 -or $parts.Count -lt $i + 3) { return $RunDir.Parent.FullName }

    $session = $parts[$i + 2]
    if ($session.Length -gt 8) { $session = $session.Substring(0, 8) }
    return "$($parts[$i + 1]) / $session"
}

# ------------------------------------------------------------------ io

# The writer still holds these files open, so a plain Get-Content can hit a sharing violation.
function Read-SharedLines {
    param([string]$Path)

    $stream = $null
    $reader = $null
    try {
        $stream = [System.IO.FileStream]::new(
            $Path,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite)
        $reader = [System.IO.StreamReader]::new($stream)
        return @($reader.ReadToEnd() -split "`r?`n" | Where-Object { $_ })
    }
    catch {
        return @()
    }
    finally {
        if ($reader) { $reader.Dispose() }
        elseif ($stream) { $stream.Dispose() }
    }
}

function ConvertFrom-JsonSafe {
    param([string]$Line)
    try { return $Line | ConvertFrom-Json } catch { return $null }
}

function Get-Excerpt {
    param([string]$Text, [int]$Max = 96)

    if (-not $Text) { return '' }
    $flat = ($Text -replace '\s+', ' ').Trim()
    if ($flat.Length -le $Max) { return $flat }
    return $flat.Substring(0, $Max - 1) + [char]0x2026
}

# ------------------------------------------------------------------ agents

# Neither journal.jsonl nor agent-<id>.meta.json records the workflow's own label for an agent
# (verified: the journal carries only type/key/agentId, the meta only agentType/spawnDepth), so the
# name has to be recovered from the opening prompt. Workflow prompts are markdown, and the
# task-specific part is conventionally its own "## ..." heading, so prefer the heading that names
# the agent's job and fall back progressively rather than assuming any one workflow's wording.
function Get-AgentLabel {
    param([string]$FirstLine)

    $first = ConvertFrom-JsonSafe $FirstLine
    if (-not $first) { return 'unknown' }

    $text = ''
    if ($first.PSObject.Properties['message'] -and $first.message.PSObject.Properties['content']) {
        $content = $first.message.content
        $text = if ($content -is [string]) { $content }
                else { ($content | ForEach-Object { if ($_.PSObject.Properties['text']) { $_.text } }) -join "`n" }
    }
    if (-not $text) { return 'unknown' }

    $headings = [regex]::Matches($text, '(?m)^#{1,3}\s+(.+?)\s*$') |
        ForEach-Object { $_.Groups[1].Value }

    # "Your job: merge ..." / "Your investigation: why does ..." - take what follows the colon.
    foreach ($h in $headings) {
        if ($h -match '^\s*Your\s+\w+\s*:\s*(.+)$') { return Get-Excerpt $Matches[1] 34 }
    }

    # Otherwise the last heading is the task-specific one; shared preamble comes first.
    if ($headings) { return Get-Excerpt $headings[-1] 34 }

    $line = ($text -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
    return Get-Excerpt $line 34
}

# Walk backwards to the newest thing the agent actually did, and describe it in one line.
function Get-LastAction {
    param([string[]]$Lines)

    for ($i = $Lines.Count - 1; $i -ge 0; $i--) {
        $entry = ConvertFrom-JsonSafe $Lines[$i]
        if (-not $entry -or $entry.type -ne 'assistant') { continue }

        $blocks = @($entry.message.content)
        for ($b = $blocks.Count - 1; $b -ge 0; $b--) {
            $block = $blocks[$b]
            if (-not $block -or -not $block.PSObject.Properties['type']) { continue }

            $when = if ($entry.PSObject.Properties['timestamp']) { $entry.timestamp } else { $null }

            switch ($block.type) {
                'tool_use' {
                    $arg = ''
                    if ($block.PSObject.Properties['input'] -and $block.input) {
                        foreach ($key in 'file_path', 'command', 'pattern', 'prompt') {
                            if ($block.input.PSObject.Properties[$key]) {
                                $arg = Get-Excerpt ([string]$block.input.$key) 70
                                break
                            }
                        }
                    }
                    return [pscustomobject]@{ Kind = $block.name; Text = $arg; When = $when }
                }
                'text'     { return [pscustomobject]@{ Kind = 'says';     Text = Get-Excerpt ([string]$block.text) 70;     When = $when } }
                'thinking' { return [pscustomobject]@{ Kind = 'thinking'; Text = Get-Excerpt ([string]$block.thinking) 70; When = $when } }
            }
        }
    }

    return [pscustomobject]@{ Kind = 'starting'; Text = ''; When = $null }
}

function Get-AgentSummary {
    param([System.IO.FileInfo]$File, [hashtable]$Status)

    $lines = Read-SharedLines $File.FullName
    if ($lines.Count -eq 0) { return $null }

    $id = $File.BaseName -replace '^agent-', ''
    $toolCalls = 0
    foreach ($line in $lines) {
        if ($line.Contains('"type":"tool_use"')) { $toolCalls++ }
    }

    $last = Get-LastAction $lines
    $age = ''
    if ($last.When) {
        $age = '{0,5:n0}s' -f ([datetime]::UtcNow - [datetime]$last.When).TotalSeconds
    }

    return [pscustomobject]@{
        Agent   = Get-AgentLabel $lines[0]
        Status  = if ($Status.ContainsKey($id)) { $Status[$id] } else { 'running' }
        Turns   = $lines.Count
        Tools   = $toolCalls
        Idle    = $age
        MB      = '{0,5:n1}' -f ($File.Length / 1MB)
        Doing   = if ($last.Text) { "$($last.Kind) $($last.Text)" } else { $last.Kind }
        Id      = $id
        Updated = $File.LastWriteTime
    }
}

# ------------------------------------------------------------------ runs

function Get-RunState {
    param([System.IO.DirectoryInfo]$RunDir, [int]$StaleMinutes)

    # journal.jsonl is the authority on lifecycle; agent transcripts only tell you about activity.
    $status = @{}
    $results = [System.Collections.Generic.List[object]]::new()
    $failures = [System.Collections.Generic.List[object]]::new()

    # MEASURED across every run on this machine: the journal only ever emits "started" and "result".
    # "completed"/"failed" do not occur - they are tolerated below only so a future format change
    # degrades gracefully rather than showing a raw event name as the state.
    $started = [System.Collections.Generic.HashSet[string]]::new()
    $returned = [System.Collections.Generic.HashSet[string]]::new()

    foreach ($line in (Read-SharedLines (Join-Path $RunDir.FullName 'journal.jsonl'))) {
        # Not $event: that is a PowerShell automatic variable.
        $ev = ConvertFrom-JsonSafe $line
        if (-not $ev -or -not $ev.PSObject.Properties['agentId']) { continue }

        $status[$ev.agentId] = switch ($ev.type) {
            'started'                        { 'running' }
            { $_ -in 'result', 'completed' } { 'done' }
            'failed'                         { 'FAILED' }
            default                          { [string]$ev.type }
        }

        if ($ev.type -eq 'started') { [void]$started.Add($ev.agentId) }

        if ($ev.type -eq 'failed') {
            $reason = if ($ev.PSObject.Properties['error']) { [string]$ev.error } else { '(no reason recorded)' }
            $failures.Add([pscustomobject]@{ Agent = $ev.agentId; Reason = Get-Excerpt $reason 140 })
        }

        if ($ev.type -in 'result', 'completed') {
            [void]$returned.Add($ev.agentId)

            if ($ev.PSObject.Properties['result']) {
                $results.Add([pscustomobject]@{
                    Agent  = $ev.agentId
                    Result = Get-Excerpt ($ev.result | ConvertTo-Json -Compress -Depth 6) 160
                })
            }
        }
    }

    # There is no "failed" event, so a dead agent is indistinguishable from a busy one except by
    # this: it started and never returned. Naming it matters, because a workflow that reports an
    # empty findings list because its agents died looks exactly like one that found nothing.
    $silent = @($started | Where-Object { -not $returned.Contains($_) })

    $agents = @(Get-ChildItem -LiteralPath $RunDir.FullName -Filter 'agent-*.jsonl' -EA SilentlyContinue |
        ForEach-Object { Get-AgentSummary $_ $status } |
        Where-Object { $_ } |
        Sort-Object Agent)

    $running = @($agents | Where-Object { $_.Status -eq 'running' }).Count
    $lastWrite = $RunDir.LastWriteTime
    foreach ($a in $agents) { if ($a.Updated -gt $lastWrite) { $lastWrite = $a.Updated } }

    $quietFor = ([datetime]::Now - $lastWrite).TotalMinutes
    $state =
        if ($running -eq 0) { 'finished' }
        elseif ($quietFor -ge $StaleMinutes) { 'stale' }
        else { 'running' }

    return [pscustomobject]@{
        Dir       = $RunDir
        Origin    = Get-RunOrigin $RunDir
        Agents    = $agents
        Running   = $running
        Done      = @($agents | Where-Object { $_.Status -ne 'running' }).Count
        Failures  = $failures
        Results   = $results
        Silent    = $silent
        State     = $state
        LastWrite = $lastWrite
        QuietMins = $quietFor
    }
}

function Show-Run {
    param([object]$Run, [switch]$Detail)

    $colour = switch ($Run.State) {
        'running'  { 'Cyan' }
        'stale'    { 'Yellow' }
        default    { 'DarkGray' }
    }

    Write-Host ("{0}  [{1}]" -f $Run.Dir.Name, $Run.State) -ForegroundColor $colour
    Write-Host ("  {0}  |  {1}/{2} agents finished  |  last write {3:HH:mm:ss}" -f
        $Run.Origin, $Run.Done, $Run.Agents.Count, $Run.LastWrite) -ForegroundColor DarkGray

    if ($Run.State -eq 'stale') {
        Write-Host ("  no transcript write for {0:n0} min - these agents are probably dead, not busy." -f $Run.QuietMins) -ForegroundColor Yellow
    }

    $Run.Agents |
        Format-Table -Property `
            @{ Label = 'Agent'; Expression = 'Agent'; Width = 36 },
            @{ Label = 'State'; Expression = 'Status'; Width = 8 },
            @{ Label = 'Turns'; Expression = 'Turns'; Width = 6 },
            @{ Label = 'Tools'; Expression = 'Tools'; Width = 6 },
            @{ Label = 'Idle'; Expression = 'Idle'; Width = 7 },
            @{ Label = 'Log MB'; Expression = 'MB'; Width = 7 },
            @{ Label = 'Currently'; Expression = 'Doing' } |
        Out-Host

    # Failures first and loudly: an empty result set reads as "found nothing" when it can mean
    # "the finders died".
    if ($Run.Failures.Count -gt 0) {
        Write-Host '  FAILED:' -ForegroundColor Red
        foreach ($f in $Run.Failures) { Write-Host "    $($f.Agent)  $($f.Reason)" -ForegroundColor Red }
    }

    # Only meaningful once the run has stopped moving; while it is running these are simply the
    # agents still working.
    if ($Run.State -ne 'running' -and $Run.Silent.Count -gt 0) {
        Write-Host ("  {0} agent(s) started and never returned - treat this run's results as INCOMPLETE:" -f $Run.Silent.Count) -ForegroundColor Red
        foreach ($s in $Run.Silent) { Write-Host "    $s" -ForegroundColor Red }
    }

    if ($Run.Results.Count -gt 0) {
        Write-Host '  Returned:' -ForegroundColor Green
        foreach ($r in $Run.Results) { Write-Host "    $($r.Agent)  $($r.Result)" -ForegroundColor DarkGray }
    }

    if ($Detail) {
        foreach ($agent in $Run.Agents) {
            Write-Host "  --- $($agent.Agent)  [$($agent.Id)] ---" -ForegroundColor Yellow
            $lines = Read-SharedLines (Join-Path $Run.Dir.FullName "agent-$($agent.Id).jsonl")
            foreach ($line in ($lines | Select-Object -Last 12)) {
                $entry = ConvertFrom-JsonSafe $line
                if (-not $entry -or $entry.type -ne 'assistant') { continue }
                foreach ($block in @($entry.message.content)) {
                    if (-not $block -or -not $block.PSObject.Properties['type']) { continue }
                    switch ($block.type) {
                        'tool_use' { Write-Host ("      -> {0}" -f $block.name) -ForegroundColor DarkCyan }
                        'text'     { Write-Host ("      {0}" -f (Get-Excerpt $block.text 140)) }
                    }
                }
            }
        }
    }

    Write-Host ''
}

function Show-All {
    param([string]$ProjectsRoot, [string]$Root, [string[]]$RunId,
          [switch]$IncludeFinished, [int]$StaleMinutes, [int]$Last, [switch]$Detail)

    $runs = @(Find-RunDirectories -ProjectsRoot $ProjectsRoot -Root $Root -RunId $RunId |
        ForEach-Object { Get-RunState $_ $StaleMinutes })

    # Rediscovered every cycle on purpose: a workflow launched while this is watching should appear
    # without a restart.
    $shown = @($runs | Where-Object { $IncludeFinished -or $_.State -ne 'finished' })
    if ($Last -gt 0) { $shown = @($shown | Select-Object -First $Last) }

    try { Clear-Host } catch { Write-Host '' }

    $active = @($runs | Where-Object { $_.State -eq 'running' }).Count
    $stale  = @($runs | Where-Object { $_.State -eq 'stale' }).Count

    Write-Host ("{0}  |  {1} run(s) discovered  |  {2} running  |  {3} stale  |  {4} finished" -f
        (Get-Date -Format 'HH:mm:ss'), $runs.Count, $active, $stale,
        @($runs | Where-Object { $_.State -eq 'finished' }).Count) -ForegroundColor Cyan
    Write-Host ''

    if ($shown.Count -eq 0) {
        Write-Host 'No active workflow runs. Use -IncludeFinished to see completed ones.' -ForegroundColor DarkGray
        Write-Host ''
    }

    foreach ($run in $shown) { Show-Run $run -Detail:$Detail }

    return $active
}

# ------------------------------------------------------------------ main

if ($Once) {
    [void](Show-All -ProjectsRoot $ProjectsRoot -Root $Root -RunId $RunId `
        -IncludeFinished:$IncludeFinished -StaleMinutes $StaleMinutes -Last $Last -Detail:$Detail)
    return
}

Write-Host 'Watching every active workflow - Ctrl+C to stop' -ForegroundColor DarkGray
while ($true) {
    $active = Show-All -ProjectsRoot $ProjectsRoot -Root $Root -RunId $RunId `
        -IncludeFinished:$IncludeFinished -StaleMinutes $StaleMinutes -Last $Last -Detail:$Detail

    if ($active -eq 0) {
        Write-Host 'Nothing is running any more.' -ForegroundColor Green
        break
    }

    Write-Host "refreshing every ${IntervalSeconds}s - Ctrl+C to stop" -ForegroundColor DarkGray
    Start-Sleep -Seconds $IntervalSeconds
}
