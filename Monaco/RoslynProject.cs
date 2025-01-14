using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OneDas.DataManagement.Monaco
{
    public class RoslynProject
    {
        public RoslynProject()
        {
            var assemblies = MefHostServices.DefaultAssemblies;
            var systemRuntimeAssembly = Assembly.Load("System.Runtime");

            // Documentation providers
            var documentationProvider = XmlDocumentationProvider.CreateFromFile(@"./Resources/System.Runtime.xml");

            // Metadata references
            var metaReferences = new List<PortableExecutableReference>()
            {
                MetadataReference.CreateFromFile(systemRuntimeAssembly.Location, documentation: documentationProvider),
            };

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x != null && x.Location != systemRuntimeAssembly.Location && x.Location.IndexOf($"dotnet{Path.DirectorySeparatorChar}shared") != -1 && x.Location.IndexOf("AspNetCore") == -1))
            {
                metaReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            var customPath = Path.GetFullPath("./Resources/Custom");
            if (Directory.Exists(customPath))
            {
                foreach (var file in Directory.GetFiles(customPath, "*.dll"))
                {
                    var dllFile = new FileInfo(file);
                    var xmlFile = new FileInfo(Path.Combine("./Resources/Custom", Path.GetFileNameWithoutExtension(dllFile.Name) + ".xml"));
                    var metadataReference = MetadataReference.CreateFromFile(dllFile.FullName, documentation: xmlFile.Exists ? XmlDocumentationProvider.CreateFromFile(xmlFile.FullName) : null);
                    metaReferences.Add(metadataReference);
                }
            }

            var host = MefHostServices.Create(assemblies);

            // workspace
            this.Workspace = new AdhocWorkspace(host);

            // Project
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Script);

            var projectInfo = ProjectInfo
                .Create(ProjectId.CreateNewId(), VersionStamp.Create(), "OneDas", "OneDas", LanguageNames.CSharp)
                .WithMetadataReferences(metaReferences)
                .WithCompilationOptions(new CSharpCompilationOptions(
                    OutputKind.ConsoleApplication, 
                    usings: new List<string>() { "System.Linq" }, 
                    sourceReferenceResolver: new SourceFileResolver(new List<string>() { Path.Combine(Path.GetFullPath("./Resources"), "Custom") }, Path.GetFullPath("./Resources")))
                )
                .WithParseOptions(parseOptions);

            var project = this.Workspace.AddProject(projectInfo);

            // code
            this.UseOnlyOnceDocument = this.Workspace.AddDocument(project.Id, "Code.cs", SourceText.From(string.Empty));
            this.DocumentId = this.UseOnlyOnceDocument.Id;
        }

        public AdhocWorkspace Workspace { get; init; }

        public Document UseOnlyOnceDocument { get; init; }

        public DocumentId DocumentId { get; init; }
    }
}