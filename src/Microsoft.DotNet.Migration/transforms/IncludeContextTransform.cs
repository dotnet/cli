using Microsoft.DotNet.ProjectModel.Files;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.DotNet.ProjectJsonMigration.Models;

namespace Microsoft.DotNet.ProjectJsonMigration.Transforms
{
    public class IncludeContextTransform : AggregateTransform<IncludeContext>
    {
        // TODO: If a directory is specified in project.json does this need to be replaced with a glob in msbuild?
        //     - Partially solved, what if the resolved glob is a directory?
        // TODO: Support mappings

        private string _itemName;
        private bool _transformMappings;
        private List<ItemMetadataValue<IncludeContext>> _metadata = new List<ItemMetadataValue<IncludeContext>>();

        public IncludeContextTransform(string itemName, bool transformMappings = true)
        {
            _itemName = itemName;
            _transformMappings = transformMappings;

            CreateTransformSet();
        }

        public IncludeContextTransform WithMetadata(string metadataName, string metadataValue)
        {
            _metadata.Add(new ItemMetadataValue<IncludeContext>(metadataName, metadataValue));
            return this;
        }

        public IncludeContextTransform WithMetadata(string metadataName, Func<IncludeContext, string> metadataValueFunc)
        {
            _metadata.Add(new ItemMetadataValue<IncludeContext>(metadataName, metadataValueFunc));
            return this;
        }

        private void CreateTransformSet()
        {
            var includeFilesExcludeFilesTransformation = new AddItemTransform<IncludeContext>(
                _itemName,
                includeContext => FormatPatterns(includeContext.IncludeFiles, includeContext.SourceBasePath),
                includeContext => FormatPatterns(includeContext.ExcludeFiles, includeContext.SourceBasePath),
                includeContext => true);

            var includeExcludeTransformation = new AddItemTransform<IncludeContext>(
                _itemName,
                includeContext => 
                {
                    var fullIncludeSet = includeContext.IncludePatterns
                                         .Concat(includeContext.BuiltInsInclude);

                    return FormatPatterns(fullIncludeSet, includeContext.SourceBasePath);
                },
                includeContext =>
                {
                    var fullExcludeSet = includeContext.ExcludePatterns
                                         .Concat(includeContext.BuiltInsExclude)
                                         .Concat(includeContext.ExcludeFiles);
                    return FormatPatterns(fullExcludeSet, includeContext.SourceBasePath);
                },
                includeContext => true);

            foreach (var metadata in _metadata)
            {
                includeFilesExcludeFilesTransformation.WithMetadata(metadata);
                includeExcludeTransformation.WithMetadata(metadata);
            }

            _transforms = new ITransform<IncludeContext>[]
            {
                includeFilesExcludeFilesTransformation,
                includeExcludeTransformation
            };
        }

        private string FormatPatterns(IEnumerable<string> patterns, string projectDirectory)
        {
            List<string> mutatedPatterns = new List<string>(patterns.Count());

            foreach (var pattern in patterns)
            {
                // Do not use forward slashes
                // https://github.com/Microsoft/msbuild/issues/724
                var mutatedPattern = pattern.Replace('/', '\\');

                // MSBuild cannot copy directories
                mutatedPattern = ReplaceDirectoriesWithGlobs(mutatedPattern, projectDirectory);

                mutatedPatterns.Add(mutatedPattern);
            }

            return string.Join(";", mutatedPatterns);
        }

        private string ReplaceDirectoriesWithGlobs(string pattern, string projectDirectory)
        {
            if (PatternIsDirectory(pattern, projectDirectory))
            {
                return $"{pattern.TrimEnd(new char[] { '\\' })}\\**\\*";
            }
        }

        private bool PatternIsDirectory(string pattern, string projectDirectory)
        {
            // TODO: what about /some/path/**/somedir?
            // Should this even be migrated?
            var path = pattern;

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(projectDirectory, path);
            }

            return Directory.Exists(path);
        }
    }
}
