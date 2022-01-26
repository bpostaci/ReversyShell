using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversyShell
{
    public class ConfigParser
    {
		public Dictionary<string, string> ConfigParameters = new Dictionary<string, string>();
		public List<string> ReferencesList = new List<string>();
		public List<string> AssemblyList = new List<string>();
		public string ConfigFilePath { get; protected set;}

		////These are application specific so could be hardcoded. 
		//private static List<string> SectionNames = new List<string>()
		//{
		//	"[config]","[referencepaths]","[assemblies]"
		//};
		enum Sections
        {
			Undefined,
			Config,
			Reference,
			Assembly
        }
        public ConfigParser(string configFilePath)
        {
            ConfigFilePath = configFilePath; 
        }

        public Result ParseConfig()
		{
			if (!File.Exists(ConfigFilePath))
				return Result.Failure($"Config Parser: Configuration file not found -> {ConfigFilePath}");

			var content = File.ReadAllText(ConfigFilePath); //If fails here exception should hanled top most exception handler.

			if (content.Length == 0)
				return Result.Failure($"Config Parser: Configuration File either empty or can not be readable-> {ConfigFilePath}"); 

			var lines = content.Split('\n'); //parse lines with newline char

			Sections cmode = Sections.Undefined;
			int LineNumber = 0;
			string currentLine = string.Empty;

			foreach (var line in lines)
			{
				LineNumber++; //Count process line numbers.

				if (line.Trim().Length == 0) continue; //pypass any empty line

				currentLine = line.Trim().Replace("\r", ""); //get rid of if /r char at the end. 

				//Find Section. Just 3 of them I don't want to use any pattern here 'if' 'else' is enough. These are application specific so could be hardcoded. 
				if (currentLine.StartsWith("[Config]", StringComparison.InvariantCultureIgnoreCase))
				{
					cmode = Sections.Config;
					continue;
				}
				else if (currentLine.StartsWith("[ReferencePaths]", StringComparison.InvariantCultureIgnoreCase))
				{
					cmode = Sections.Reference;
					continue;
				}
				else if (currentLine.StartsWith("[Assemblies]", StringComparison.InvariantCultureIgnoreCase))
				{
					cmode = Sections.Assembly;
					continue;
				}
				else if (currentLine.StartsWith("["))
				{
					return Result.Failure($"Config Parser: Line:{LineNumber} Unknown Section Name");
				}

				switch(cmode)
				{
					case Sections.Config:

						if(!currentLine.Contains("="))
							return Result.Failure($"Config Parser: Line:{LineNumber} Incorrect key, value pair");

						var keyvalue = currentLine.Split('=');
							if (keyvalue.Length != 2)
								return Result.Failure($"Config Parser: Line:{LineNumber} Incorrect key, value pair");

						ConfigParameters.Add(keyvalue[0].Trim().ToLower(), keyvalue[1].Trim());

					break;

					case Sections.Reference:

							if (!Directory.Exists(currentLine))
								return Result.Failure($"Config Parser: Line:{LineNumber} Reference Directory Not Found");

							if(!ReferencesList.Contains(currentLine)) //Ignore if already added.
								ReferencesList.Add(currentLine);
						
					break;

					case Sections.Assembly:
				
					if (!(currentLine.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) || currentLine.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)))
						return Result.Failure($"Config Parser: Line:{LineNumber} Incorrect assembly name should be a dll or exe");
					
						if(!AssemblyList.Contains(currentLine)) //Ignore if already added.
							AssemblyList.Add(currentLine);
						
					break;
				}

			}

			//Check Health and validate

			if (ReferencesList.Count == 0) return Result.Failure("Config Parser: At least one reference path defined in [ReferencePaths] section in config file");
			if (AssemblyList.Count == 0) return Result.Failure("Config Parser: At least one assembly defined in [Assemblies] section in config file");
			if (!ConfigParameters.ContainsKey("workspace")) return Result.Failure("Config Parser: workspace should be defined in [Config] section in config file");

			return Result.Success();
		}
    }
}
