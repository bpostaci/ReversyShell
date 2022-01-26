using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Drawing;
using CSharpFunctionalExtensions;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;

namespace ReversyShell
{
    public class Decompiler
    {
		private ConfigParser config;
        public Decompiler(ConfigParser parser)
        {
			config = parser;
			WorkspacePath = parser.ConfigParameters["workspace"];
			ReferencePaths = parser.ReferencesList.ToArray();
			AssemblyList = parser.AssemblyList.ToArray(); 

        }

		public string WorkspacePath { get; set; }

		//[FileExists]
		//[Required]
		//[Argument(0, "Assembly file name", "The assembly that is being decompiled. This argument is mandatory.")]
		public string InputAssemblyName { get; }

		//[DirectoryExists]
		////[Option("-o|--outputdir <directory>", "The output directory, if omitted decompiler output is written to standard out.", CommandOptionType.SingleValue)]
		//public string OutputDirectory { get; }

		//[Option("-p|--project", "Decompile assembly as compilable project. This requires the output directory option.", CommandOptionType.NoValue)]
		//public bool CreateCompilableProjectFlag { get; }

		//[Option("-t|--type <type-name>", "The fully qualified name of the type to decompile.", CommandOptionType.SingleValue)]
		//public string TypeName { get; }

		//[Option("-il|--ilcode", "Show IL code.", CommandOptionType.NoValue)]
		//public bool ShowILCodeFlag { get; }

		//[Option("--il-sequence-points", "Show IL with sequence points. Implies -il.", CommandOptionType.NoValue)]
		public bool ShowILSequencePointsFlag { get; }

		//[Option("-genpdb", "Generate PDB.", CommandOptionType.NoValue)]
		//public bool CreateDebugInfoFlag { get; }

		//[FileExistsOrNull]
		//[Option("-usepdb", "Use PDB.", CommandOptionType.SingleOrNoValue)]
		public (bool IsSet, string Value) InputPDBFile { get; }

		//[Option("-l|--list <entity-type(s)>", "Lists all entities of the specified type(s). Valid types: c(lass), i(nterface), s(truct), d(elegate), e(num)", CommandOptionType.MultipleValue)]
		//public string[] EntityTypes { get; } = new string[0];

		//[Option("-v|--version", "Show version of ICSharpCode.Decompiler used.", CommandOptionType.NoValue)]
		//public bool ShowVersion { get; }

		//[Option("-lv|--languageversion <version>", "C# Language version: CSharp1, CSharp2, CSharp3, CSharp4, CSharp5, CSharp6, CSharp7_0, CSharp7_1, CSharp7_2, CSharp7_3, CSharp8_0 or Latest", CommandOptionType.SingleValue)]
		public LanguageVersion LanguageVersion { get; set;  } = LanguageVersion.Latest;

		//[DirectoryExists]
		//[Option("-r|--referencepath <path>", "Path to a directory containing dependencies of the assembly that is being decompiled.", CommandOptionType.MultipleValue)]
		public string[] ReferencePaths { get; set; }
		public string[] AssemblyList { get; set; }

		//[Option("--no-dead-code", "Remove dead code.", CommandOptionType.NoValue)]
		public bool RemoveDeadCode { get; }

		//[Option("--no-dead-stores", "Remove dead stores.", CommandOptionType.NoValue)]
		public bool RemoveDeadStores { get; }
		//private int OnExecute(CommandLineApplication app)
		//{

		public Result Execute()
        {

			if (!Directory.Exists(WorkspacePath))
				return Result.Failure("Workspace folder not found"); 

			foreach(var item in AssemblyList)
            {
				string asmPath = Path.Combine(WorkspacePath, item);

				if(!File.Exists(asmPath))
                {
					Colorful.Console.WriteLine($" {item} is not found in the workspace , skipping this assembly" , Color.Yellow);
					continue; 
                }
				string asmNameNoExt = Path.GetFileNameWithoutExtension(asmPath);
				string outputdir = Path.Combine(WorkspacePath, "src", asmNameNoExt);
				if(!Directory.Exists(outputdir))
					Directory.CreateDirectory(outputdir);


				try
				{
					Colorful.Console.Write($"Generating Project for {item} ");
					/*3rdparty*/ DecompileAsProject(asmPath, outputdir);
					PrintStatusSuccess(); 
				}
				catch(Exception ex)
                {
					PrintStatusFailure();

					Colorful.Console.WriteLine($"Error: Unable to generate project for assembly: {item}", Color.Red);
					Colorful.Console.WriteLine(ex.Message);
				}

				try
				{
					Colorful.Console.Write($"Generating portable symbol file (PDB) for {item} ");
					var pdbPath =  Path.ChangeExtension(asmPath, ".pdb");
					/*3rdparty*/ GeneratePdbForAssembly(asmPath, pdbPath);

					PrintStatusSuccess();
				}
				catch(Exception ex)
                {
					PrintStatusFailure();
				}

			}
			return Result.Success(); 
        }

		private void PrintStatusSuccess()
        {
			Colorful.Console.Write($"[");
			Colorful.Console.Write($"Success", Color.Green);
			Colorful.Console.WriteLine($"]");
		}
		private void PrintStatusFailure()
        {
			Colorful.Console.Write($"[");
			Colorful.Console.Write($"Failed", Color.Red);
			Colorful.Console.WriteLine($"]");
		}

		//	TextWriter output = System.Console.Out;
		//	bool outputDirectorySpecified = !string.IsNullOrEmpty(OutputDirectory);

		//	try
		//	{
		//		if (CreateCompilableProjectFlag)
		//		{
		//			return DecompileAsProject(InputAssemblyName, OutputDirectory);
		//		}


		//		else if (CreateDebugInfoFlag)
		//		{
		//			string pdbFileName = null;
		//			if (outputDirectorySpecified)
		//			{
		//				string outputName = Path.GetFileNameWithoutExtension(InputAssemblyName);
		//				pdbFileName = Path.Combine(OutputDirectory, outputName) + ".pdb";
		//			}
		//			else
		//			{
		//				pdbFileName = Path.ChangeExtension(InputAssemblyName, ".pdb");
		//			}

		//			return GeneratePdbForAssembly(InputAssemblyName, pdbFileName, app);
		//		}
		//		else if (ShowVersion)
		//		{
		//			string vInfo = "reversycmd: " + typeof(ReversyCmdProgram).Assembly.GetName().Version.ToString() +
		//						   Environment.NewLine
		//						   + "ICSharpCode.Decompiler: " +
		//						   typeof(FullTypeName).Assembly.GetName().Version.ToString();
		//			output.WriteLine(vInfo);
		//		}

		//	}
		//	catch (Exception ex)
		//	{
		//		app.Error.WriteLine(ex.ToString());
		//		return ProgramExitCodes.EX_SOFTWARE;
		//	}
		//	finally
		//	{
		//		output.Close();
		//	}

		//	return 0;
		//}

		DecompilerSettings GetSettings(PEFile module)
		{
			return new DecompilerSettings(LanguageVersion)
			{
				ThrowOnAssemblyResolveErrors = false,
				RemoveDeadCode = RemoveDeadCode,
				RemoveDeadStores = RemoveDeadStores,
				//UseNestedDirectoriesForNamespaces = true,
				//UseRootFolderAtNestedDirectoriesForNamespaces = true,
				UseSdkStyleProjectFormat = WholeProjectDecompiler.CanUseSdkStyleProjectFormat(module),

			};
		}

		CSharpDecompiler GetDecompiler(string assemblyFileName)
		{
			var module = new PEFile(assemblyFileName);
			var resolver = new UniversalAssemblyResolver(assemblyFileName, false, module.Reader.DetectTargetFrameworkId());
			foreach (var path in ReferencePaths)
			{
				resolver.AddSearchDirectory(path);
			}
			return new CSharpDecompiler(assemblyFileName, resolver, GetSettings(module))
			{
				DebugInfoProvider = TryLoadPDB(module)
			};
		}

		int ListContent(string assemblyFileName, TextWriter output, ISet<TypeKind> kinds)
		{
			CSharpDecompiler decompiler = GetDecompiler(assemblyFileName);

			foreach (var type in decompiler.TypeSystem.MainModule.TypeDefinitions)
			{
				if (!kinds.Contains(type.Kind))
					continue;
				output.WriteLine($"{type.Kind} {type.FullName}");
			}
			return 0;
		}

		int ShowIL(string assemblyFileName, TextWriter output)
		{
			var module = new PEFile(assemblyFileName);
			output.WriteLine($"// IL code: {module.Name}");
			var disassembler = new ReflectionDisassembler(new PlainTextOutput(output), CancellationToken.None)
			{
				DebugInfo = TryLoadPDB(module),
				ShowSequencePoints = ShowILSequencePointsFlag,
			};
			disassembler.WriteModuleContents(module);
			return 0;
		}

		int DecompileAsProject(string assemblyFileName, string outputDirectory)
		{
			var module = new PEFile(assemblyFileName);
			var resolver = new UniversalAssemblyResolver(assemblyFileName, false, module.Reader.DetectTargetFrameworkId());
			foreach (var path in ReferencePaths)
			{
				resolver.AddSearchDirectory(path);
			}
			var decompiler = new WholeProjectDecompiler(GetSettings(module), resolver, resolver, TryLoadPDB(module));
			decompiler.DecompileProject(module, outputDirectory);
			return 0;
		}

		int Decompile(string assemblyFileName, TextWriter output, string typeName = null)
		{
			CSharpDecompiler decompiler = GetDecompiler(assemblyFileName);

			if (typeName == null)
			{
				output.Write(decompiler.DecompileWholeModuleAsString());
			}
			else
			{
				var name = new FullTypeName(typeName);
				output.Write(decompiler.DecompileTypeAsString(name));
			}
			return 0;
		}

		int GeneratePdbForAssembly(string assemblyFileName, string pdbFileName)
		{
			var module = new PEFile(assemblyFileName,
				new FileStream(assemblyFileName, FileMode.Open, FileAccess.Read),
				PEStreamOptions.PrefetchEntireImage,
				metadataOptions: MetadataReaderOptions.None);

			if (!PortablePdbWriter.HasCodeViewDebugDirectoryEntry(module))
			{
				Colorful.Console.WriteLine($"Cannot create PDB file for {assemblyFileName}, because it does not contain a PE Debug Directory Entry of type 'CodeView'.",Color.Red);
				return ProgramExitCodes.EX_DATAERR;
			}

			using (FileStream stream = new FileStream(pdbFileName, FileMode.OpenOrCreate, FileAccess.Write))
			{
				var decompiler = GetDecompiler(assemblyFileName);
				PortablePdbWriter.WritePdb(module, decompiler, GetSettings(module), stream);
			}

			return 0;
		}

		IDebugInfoProvider TryLoadPDB(PEFile module)
		{
			if (InputPDBFile.IsSet)
			{
				if (InputPDBFile.Value == null)
					return DebugInfoUtils.LoadSymbols(module);
				return DebugInfoUtils.FromFile(module, InputPDBFile.Value);
			}

			return null;
		}
	}
}
