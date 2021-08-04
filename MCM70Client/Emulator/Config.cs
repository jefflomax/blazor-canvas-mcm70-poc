namespace MCM70Client.Emulator
{
	public class Config
	{
		public Config(string[] args)
		{
			UpdateFrequency = 500;
			foreach (var arg in args)
			{
				switch (arg.ToLower())
				{
					case "blink":
						AllesLookensgepeepers = true;
						break;
					case "showops":
						ShowOpCodeListing = true;
						break;
					case "fast":
						UpdateFrequency = 0;
						break;
				}
			}
		}

		public bool AllesLookensgepeepers { get; set; }
		public bool AutoStart {get; set;}
		public bool AutoPrinter {get; set; }
		public string AutoMountTape0 { get; set; }
		public string AutoMountTape1 { get; set; }
		public bool EmitPrintingToTextFile { get; set; }
		public int UpdateFrequency { get; set; }
		public bool ShowOpCodeListing { get; set; }
		public bool ShowDisassembly { get; set; }
		public bool InstructionReordering { get; set; }
		public bool SpeculativeExecution { get; set; }
	}
}
