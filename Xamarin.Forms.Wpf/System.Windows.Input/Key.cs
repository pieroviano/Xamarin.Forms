namespace System.Windows.Input
{
	/// <summary>WPF's keys, with WPF's values.</summary>
	public enum Key
	{
		None = 0,
		Cancel = 1,
		Back = 2,
		Tab = 3,
		LineFeed = 4,
		Clear = 5,
		Return = 6,
		Enter = 6,
		Pause = 7,
		Capital = 8,
		CapsLock = 8,
		HangulMode = 9,
		KanaMode = 9,
		JunjaMode = 10,
		FinalMode = 11,
		HanjaMode = 12,
		KanjiMode = 12,
		Escape = 13,
		ImeConvert = 14,
		ImeNonConvert = 15,
		ImeAccept = 16,
		ImeModeChange = 17,
		Space = 18,
		PageUp = 19,
		Prior = 19,
		Next = 20,
		PageDown = 20,
		End = 21,
		Home = 22,
		Left = 23,
		Up = 24,
		Right = 25,
		Down = 26,
		Select = 27,
		Print = 28,
		Execute = 29,
		PrintScreen = 30,
		Snapshot = 30,
		Insert = 31,
		Delete = 32,
		Help = 33,
		D0 = 34,
		D1 = 35,
		D2 = 36,
		D3 = 37,
		D4 = 38,
		D5 = 39,
		D6 = 40,
		D7 = 41,
		D8 = 42,
		D9 = 43,
		A = 44,
		B = 45,
		C = 46,
		D = 47,
		E = 48,
		F = 49,
		G = 50,
		H = 51,
		I = 52,
		J = 53,
		K = 54,
		L = 55,
		M = 56,
		N = 57,
		O = 58,
		P = 59,
		Q = 60,
		R = 61,
		S = 62,
		T = 63,
		U = 64,
		V = 65,
		W = 66,
		X = 67,
		Y = 68,
		Z = 69,
		LWin = 70,
		RWin = 71,
		Apps = 72,
		Sleep = 73,
		NumPad0 = 74,
		NumPad1 = 75,
		NumPad2 = 76,
		NumPad3 = 77,
		NumPad4 = 78,
		NumPad5 = 79,
		NumPad6 = 80,
		NumPad7 = 81,
		NumPad8 = 82,
		NumPad9 = 83,
		Multiply = 84,
		Add = 85,
		Separator = 86,
		Subtract = 87,
		Decimal = 88,
		Divide = 89,
		F1 = 90,
		F2 = 91,
		F3 = 92,
		F4 = 93,
		F5 = 94,
		F6 = 95,
		F7 = 96,
		F8 = 97,
		F9 = 98,
		F10 = 99,
		F11 = 100,
		F12 = 101,
		F13 = 102,
		F14 = 103,
		F15 = 104,
		F16 = 105,
		F17 = 106,
		F18 = 107,
		F19 = 108,
		F20 = 109,
		F21 = 110,
		F22 = 111,
		F23 = 112,
		F24 = 113,
		NumLock = 114,
		Scroll = 115,
		LeftShift = 116,
		RightShift = 117,
		LeftCtrl = 118,
		RightCtrl = 119,
		LeftAlt = 120,
		RightAlt = 121,
		BrowserBack = 122,
		BrowserForward = 123,
		BrowserRefresh = 124,
		BrowserStop = 125,
		BrowserSearch = 126,
		BrowserFavorites = 127,
		BrowserHome = 128,
		VolumeMute = 129,
		VolumeDown = 130,
		VolumeUp = 131,
		MediaNextTrack = 132,
		MediaPreviousTrack = 133,
		MediaStop = 134,
		MediaPlayPause = 135,
		LaunchMail = 136,
		SelectMedia = 137,
		LaunchApplication1 = 138,
		LaunchApplication2 = 139,
		Oem1 = 140,
		OemSemicolon = 140,
		OemPlus = 141,
		OemComma = 142,
		OemMinus = 143,
		OemPeriod = 144,
		Oem2 = 145,
		OemQuestion = 145,
		Oem3 = 146,
		OemTilde = 146,
		AbntC1 = 147,
		AbntC2 = 148,
		Oem4 = 149,
		OemOpenBrackets = 149,
		Oem5 = 150,
		OemPipe = 150,
		Oem6 = 151,
		OemCloseBrackets = 151,
		Oem7 = 152,
		OemQuotes = 152,
		Oem8 = 153,
		Oem102 = 154,
		OemBackslash = 154,
		ImeProcessed = 155,
		System = 156,
		DbeAlphanumeric = 157,
		OemAttn = 157,
		DbeKatakana = 158,
		OemFinish = 158,
		DbeHiragana = 159,
		OemCopy = 159,
		DbeSbcsChar = 160,
		OemAuto = 160,
		DbeDbcsChar = 161,
		OemEnlw = 161,
		DbeRoman = 162,
		OemBackTab = 162,
		Attn = 163,
		DbeNoRoman = 163,
		CrSel = 164,
		DbeEnterWordRegisterMode = 164,
		DbeEnterImeConfigureMode = 165,
		ExSel = 165,
		DbeFlushString = 166,
		EraseEof = 166,
		DbeCodeInput = 167,
		Play = 167,
		DbeNoCodeInput = 168,
		Zoom = 168,
		DbeDetermineString = 169,
		NoName = 169,
		DbeEnterDialogConversionMode = 170,
		Pa1 = 170,
		OemClear = 171,
		DeadCharProcessed = 172,
	}

	[Flags]
	public enum ModifierKeys
	{
		None = 0,
		Alt = 1,
		Control = 2,
		Shift = 4,
		Windows = 8,
	}

	[Flags]
	public enum KeyStates : byte
	{
		None = 0,
		Down = 1,
		Toggled = 2,
	}

	/// <summary>WPF keys to Win32 virtual-key codes and back, with WPF's own table.</summary>
	public static class KeyInterop
	{
		static readonly int[] s_virtualKeys = BuildVirtualKeys();

		public static int VirtualKeyFromKey(Key key)
		{
			var i = (int)key;
			return i >= 0 && i < s_virtualKeys.Length ? s_virtualKeys[i] : 0;
		}

		public static Key KeyFromVirtualKey(int virtualKey)
		{
			switch (virtualKey)
			{
				case 0x10:
					return Key.LeftShift;
				case 0x11:
					return Key.LeftCtrl;
				case 0x12:
					return Key.LeftAlt;
			}

			for (var i = 1; i < s_virtualKeys.Length; i++)
			{
				if (s_virtualKeys[i] == virtualKey)
					return (Key)i;
			}

			return Key.None;
		}

		static int[] BuildVirtualKeys()
		{
			var vk = new int[(int)Key.DeadCharProcessed + 1];

			void Set(Key key, int code) => vk[(int)key] = code;

			Set(Key.Cancel, 0x03);
			Set(Key.Back, 0x08);
			Set(Key.Tab, 0x09);
			Set(Key.Clear, 0x0C);
			Set(Key.Return, 0x0D);
			Set(Key.Pause, 0x13);
			Set(Key.Capital, 0x14);
			Set(Key.KanaMode, 0x15);
			Set(Key.JunjaMode, 0x17);
			Set(Key.FinalMode, 0x18);
			Set(Key.HanjaMode, 0x19);
			Set(Key.Escape, 0x1B);
			Set(Key.ImeConvert, 0x1C);
			Set(Key.ImeNonConvert, 0x1D);
			Set(Key.ImeAccept, 0x1E);
			Set(Key.ImeModeChange, 0x1F);
			Set(Key.Space, 0x20);
			Set(Key.Prior, 0x21);
			Set(Key.Next, 0x22);
			Set(Key.End, 0x23);
			Set(Key.Home, 0x24);
			Set(Key.Left, 0x25);
			Set(Key.Up, 0x26);
			Set(Key.Right, 0x27);
			Set(Key.Down, 0x28);
			Set(Key.Select, 0x29);
			Set(Key.Print, 0x2A);
			Set(Key.Execute, 0x2B);
			Set(Key.Snapshot, 0x2C);
			Set(Key.Insert, 0x2D);
			Set(Key.Delete, 0x2E);
			Set(Key.Help, 0x2F);
			for (var i = 0; i <= 9; i++)
				Set(Key.D0 + i, 0x30 + i);
			for (var i = 0; i < 26; i++)
				Set(Key.A + i, 0x41 + i);
			Set(Key.LWin, 0x5B);
			Set(Key.RWin, 0x5C);
			Set(Key.Apps, 0x5D);
			Set(Key.Sleep, 0x5F);
			for (var i = 0; i <= 9; i++)
				Set(Key.NumPad0 + i, 0x60 + i);
			Set(Key.Multiply, 0x6A);
			Set(Key.Add, 0x6B);
			Set(Key.Separator, 0x6C);
			Set(Key.Subtract, 0x6D);
			Set(Key.Decimal, 0x6E);
			Set(Key.Divide, 0x6F);
			for (var i = 0; i < 24; i++)
				Set(Key.F1 + i, 0x70 + i);
			Set(Key.NumLock, 0x90);
			Set(Key.Scroll, 0x91);
			Set(Key.LeftShift, 0xA0);
			Set(Key.RightShift, 0xA1);
			Set(Key.LeftCtrl, 0xA2);
			Set(Key.RightCtrl, 0xA3);
			Set(Key.LeftAlt, 0xA4);
			Set(Key.RightAlt, 0xA5);
			for (var i = 0; i < 7; i++)
				Set(Key.BrowserBack + i, 0xA6 + i);
			Set(Key.VolumeMute, 0xAD);
			Set(Key.VolumeDown, 0xAE);
			Set(Key.VolumeUp, 0xAF);
			Set(Key.MediaNextTrack, 0xB0);
			Set(Key.MediaPreviousTrack, 0xB1);
			Set(Key.MediaStop, 0xB2);
			Set(Key.MediaPlayPause, 0xB3);
			Set(Key.LaunchMail, 0xB4);
			Set(Key.SelectMedia, 0xB5);
			Set(Key.LaunchApplication1, 0xB6);
			Set(Key.LaunchApplication2, 0xB7);
			Set(Key.Oem1, 0xBA);
			Set(Key.OemPlus, 0xBB);
			Set(Key.OemComma, 0xBC);
			Set(Key.OemMinus, 0xBD);
			Set(Key.OemPeriod, 0xBE);
			Set(Key.Oem2, 0xBF);
			Set(Key.Oem3, 0xC0);
			Set(Key.AbntC1, 0xC1);
			Set(Key.AbntC2, 0xC2);
			Set(Key.Oem4, 0xDB);
			Set(Key.Oem5, 0xDC);
			Set(Key.Oem6, 0xDD);
			Set(Key.Oem7, 0xDE);
			Set(Key.Oem8, 0xDF);
			Set(Key.Oem102, 0xE2);
			Set(Key.ImeProcessed, 0xE5);
			Set(Key.System, 0x12);
			Set(Key.OemAttn, 0xF0);
			Set(Key.OemFinish, 0xF1);
			Set(Key.OemCopy, 0xF2);
			Set(Key.OemAuto, 0xF3);
			Set(Key.OemEnlw, 0xF4);
			Set(Key.OemBackTab, 0xF5);
			Set(Key.Attn, 0xF6);
			Set(Key.CrSel, 0xF7);
			Set(Key.ExSel, 0xF8);
			Set(Key.EraseEof, 0xF9);
			Set(Key.Play, 0xFA);
			Set(Key.Zoom, 0xFB);
			Set(Key.NoName, 0xFC);
			Set(Key.Pa1, 0xFD);
			Set(Key.OemClear, 0xFE);
			return vk;
		}
	}
}
