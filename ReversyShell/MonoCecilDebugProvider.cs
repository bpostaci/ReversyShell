using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;

using Mono.Cecil;
using Mono.Cecil.Pdb;

using SRM = System.Reflection.Metadata;

namespace ReversyShell
{
	public class MonoCecilDebugInfoProvider : IDebugInfoProvider
	{
		readonly Dictionary<SRM.MethodDefinitionHandle, (IList<SequencePoint> SequencePoints, IList<Variable> Variables)> debugInfo;

		public unsafe MonoCecilDebugInfoProvider(PEFile module, string pdbFileName, string description = null)
		{
			if (module == null)
			{
				throw new ArgumentNullException(nameof(module));
			}

			if (!module.Reader.IsEntireImageAvailable)
			{
				throw new ArgumentException("This provider needs access to the full image!");
			}

			this.Description = description ?? $"Loaded from PDB file: {pdbFileName}";
			this.SourceFileName = pdbFileName;

			var image = module.Reader.GetEntireImage();
			this.debugInfo = new Dictionary<SRM.MethodDefinitionHandle, (IList<SequencePoint> SequencePoints, IList<Variable> Variables)>();
			using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream(image.Pointer, image.Length))
			using (var moduleDef = ModuleDefinition.ReadModule(stream))
			{
				moduleDef.ReadSymbols(new PdbReaderProvider().GetSymbolReader(moduleDef, pdbFileName));

				foreach (var method in module.Metadata.MethodDefinitions)
				{
					var cecilMethod = moduleDef.LookupToken(MetadataTokens.GetToken(method)) as MethodDefinition;
					var debugInfo = cecilMethod?.DebugInformation;
					if (debugInfo == null)
						continue;
					IList<SequencePoint> sequencePoints = EmptyList<SequencePoint>.Instance;
					if (debugInfo.HasSequencePoints)
					{
						sequencePoints = new List<SequencePoint>(debugInfo.SequencePoints.Count);
						foreach (var point in debugInfo.SequencePoints)
						{
							sequencePoints.Add(new SequencePoint
							{
								Offset = point.Offset,
								StartLine = point.StartLine,
								StartColumn = point.StartColumn,
								EndLine = point.EndLine,
								EndColumn = point.EndColumn,
								DocumentUrl = point.Document.Url
							});
						}
					}
					var variables = new List<Variable>();
					foreach (var scope in debugInfo.GetScopes())
					{
						if (!scope.HasVariables)
							continue;
						foreach (var v in scope.Variables)
						{
							variables.Add(new Variable(v.Index, v.Name));
						}
					}
					this.debugInfo.Add(method, (sequencePoints, variables));
				}
			}
		}

		public string Description { get; }

		public string SourceFileName { get; }

		public IList<SequencePoint> GetSequencePoints(SRM.MethodDefinitionHandle handle)
		{
			if (!debugInfo.TryGetValue(handle, out var info))
			{
				return EmptyList<SequencePoint>.Instance;
			}

			return info.SequencePoints;
		}

		public IList<Variable> GetVariables(SRM.MethodDefinitionHandle handle)
		{
			if (!debugInfo.TryGetValue(handle, out var info))
			{
				return EmptyList<Variable>.Instance;
			}

			return info.Variables;
		}

		public bool TryGetName(SRM.MethodDefinitionHandle handle, int index, out string name)
		{
			name = null;
			if (!debugInfo.TryGetValue(handle, out var info))
			{
				return false;
			}

			var variable = info.Variables.FirstOrDefault(v => v.Index == index);
			name = variable.Name;
			return name != null;
		}
	}
}
