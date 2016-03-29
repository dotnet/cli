// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Utilities;
using Microsoft.Extensions.JsonParser.Sources;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Microsoft.DotNet.ProjectModel.Graph
{
    public static class LockFileReader
    {
        public static LockFile Read(string lockFilePath, bool patchWithExportFile = true)
        {
            using (var stream = ResilientFileStreamOpener.OpenFile(lockFilePath))
            {
                try
                {
                    return Read(lockFilePath, stream, patchWithExportFile);
                }
                catch (FileFormatException ex)
                {
                    throw ex.WithFilePath(lockFilePath);
                }
                catch (Exception ex)
                {
                    throw FileFormatException.Create(ex, lockFilePath);
                }
            }
        }

        public static LockFile Read(string lockFilePath, Stream stream, bool patchWithExportFile = true)
        {
            try
            {
                var reader = new StreamReader(stream);
                var jobject = JsonDeserializer.Deserialize(reader) as JsonObject;

                if (jobject == null)
                {
                    throw new InvalidDataException();
                }

                var lockFile = ReadLockFile(lockFilePath, jobject);

                var patcher = new LockFilePatcher(lockFile);

                if (patchWithExportFile)
                {
                    patcher.PatchIfNecessary();
                }
                else
                {
                    patcher.ThrowIfAnyMsbuildLibrariesPresent();
                }

                return lockFile;
            }
            catch (LockFilePatchingException)
            {
                throw;
            }
            catch
            {
                // Ran into parsing errors, mark it as unlocked and out-of-date
                return new LockFile(lockFilePath)
                {
                    Version = int.MinValue
                };
            }
        }

        public static ExportFile ReadExportFile(string fragmentLockFilePath)
        {
            using (var stream = ResilientFileStreamOpener.OpenFile(fragmentLockFilePath))
            {
                try
                {
                    var rootJObject = JsonDeserializer.Deserialize(new StreamReader(stream)) as JsonObject;

                    if (rootJObject == null)
                    {
                        throw new InvalidDataException();
                    }

                    var version = ReadInt(rootJObject, "version", defaultValue: int.MinValue);
                    var exports = ReadObject(rootJObject.ValueAsJsonObject("exports"), ReadTargetLibrary);

                    return new ExportFile(fragmentLockFilePath, version, exports);

                }
                catch (FileFormatException ex)
                {
                    throw ex.WithFilePath(fragmentLockFilePath);
                }
                catch (Exception ex)
                {
                    throw FileFormatException.Create(ex, fragmentLockFilePath);
                }
            }
        }

        private static LockFile ReadLockFile(string lockFilePath, JsonObject cursor)
        {
            var lockFile = new LockFile(lockFilePath);
            lockFile.Version = ReadInt(cursor, "version", defaultValue: int.MinValue);
            lockFile.Targets = ReadObject(cursor.ValueAsJsonObject("targets"), ReadTarget);
            lockFile.ProjectFileDependencyGroups = ReadObject(cursor.ValueAsJsonObject("projectFileDependencyGroups"), ReadProjectFileDependencyGroup);
            ReadLibrary(cursor.ValueAsJsonObject("libraries"), lockFile);

            return lockFile;
        }

        private static void ReadLibrary(JsonObject json, LockFile lockFile)
        {
            if (json == null)
            {
                return;
            }

            foreach (var key in json.Keys)
            {
                var value = json.ValueAsJsonObject(key);
                if (value == null)
                {
                    throw FileFormatException.Create("The value type is not object.", json.Value(key));
                }

                var parts = key.Split(new[] { '/' }, 2);
                var name = parts[0];
                var version = parts.Length == 2 ? NuGetVersion.Parse(parts[1]) : null;

                var type = value.ValueAsString("type")?.Value;

                if (type == null || string.Equals(type, "package", StringComparison.OrdinalIgnoreCase))
                {
                    lockFile.PackageLibraries.Add(new LockFilePackageLibrary
                    {
                        Name = name,
                        Version = version,
                        IsServiceable = ReadBool(value, "serviceable", defaultValue: false),
                        Sha512 = ReadString(value.Value("sha512")),
                        Files = ReadPathArray(value.Value("files"), ReadString)
                    });
                }
                else if (type == "project")
                {
                    var projectLibrary = new LockFileProjectLibrary
                    {
                        Name = name,
                        Version = version
                    };

                    var pathValue = value.Value("path");
                    projectLibrary.Path = pathValue == null ? null : ReadString(pathValue);

                    var buildTimeDependencyValue = value.Value("msbuildProject");
                    projectLibrary.MSBuildProject = buildTimeDependencyValue == null ? null : ReadString(buildTimeDependencyValue);

                    lockFile.ProjectLibraries.Add(projectLibrary);
                }
            }
        }

        private static LockFileTarget ReadTarget(string property, JsonValue json)
        {
            var jobject = json as JsonObject;
            if (jobject == null)
            {
                throw FileFormatException.Create("The value type is not an object.", json);
            }

            var target = new LockFileTarget();
            var parts = property.Split(new[] { '/' }, 2);
            target.TargetFramework = NuGetFramework.Parse(parts[0]);
            if (parts.Length == 2)
            {
                target.RuntimeIdentifier = parts[1];
            }

            target.Libraries = ReadObject(jobject, ReadTargetLibrary);

            return target;
        }

        private static LockFileTargetLibrary ReadTargetLibrary(string property, JsonValue json)
        {
            var jobject = json as JsonObject;
            if (jobject == null)
            {
                throw FileFormatException.Create("The value type is not an object.", json);
            }

            var library = new LockFileTargetLibrary();

            var parts = property.Split(new[] { '/' }, 2);
            library.Name = parts[0];
            if (parts.Length == 2)
            {
                library.Version = NuGetVersion.Parse(parts[1]);
            }

            library.Type = jobject.ValueAsString("type");
            var framework = jobject.ValueAsString("framework");
            if (framework != null)
            {
                library.TargetFramework = NuGetFramework.Parse(framework);
            }

            library.Dependencies = ReadObject(jobject.ValueAsJsonObject("dependencies"), ReadPackageDependency);
            library.FrameworkAssemblies = new HashSet<string>(ReadArray(jobject.Value("frameworkAssemblies"), ReadFrameworkAssemblyReference), StringComparer.OrdinalIgnoreCase);
            library.RuntimeAssemblies = ReadObject(jobject.ValueAsJsonObject("runtime"), ReadFileItem);
            library.CompileTimeAssemblies = ReadObject(jobject.ValueAsJsonObject("compile"), ReadFileItem);
            library.ResourceAssemblies = ReadObject(jobject.ValueAsJsonObject("resource"), ReadFileItem);
            library.NativeLibraries = ReadObject(jobject.ValueAsJsonObject("native"), ReadFileItem);
            library.ContentFiles = ReadObject(jobject.ValueAsJsonObject("contentFiles"), ReadContentFile);
            library.RuntimeTargets = ReadObject(jobject.ValueAsJsonObject("runtimeTargets"), ReadRuntimeTarget);
            return library;
        }

        private static LockFileRuntimeTarget ReadRuntimeTarget(string property, JsonValue json)
        {
            var jsonObject = json as JsonObject;
            if (jsonObject == null)
            {
                throw FileFormatException.Create("The value type is not an object.", json);
            }

            return new LockFileRuntimeTarget(
                path: property,
                runtime: jsonObject.ValueAsString("rid"),
                assetType: jsonObject.ValueAsString("assetType")
                );
        }

        private static LockFileContentFile ReadContentFile(string property, JsonValue json)
        {
            var contentFile = new LockFileContentFile()
            {
                Path = property
            };

            var jsonObject = json as JsonObject;
            if (jsonObject != null)
            {

                BuildAction action;
                BuildAction.TryParse(jsonObject.ValueAsString("buildAction"), out action);

                contentFile.BuildAction = action;
                var codeLanguage = jsonObject.ValueAsString("codeLanguage");
                if (codeLanguage == "any")
                {
                    codeLanguage = null;
                }
                contentFile.CodeLanguage = codeLanguage;
                contentFile.OutputPath = jsonObject.ValueAsString("outputPath");
                contentFile.PPOutputPath = jsonObject.ValueAsString("ppOutputPath");
                contentFile.CopyToOutput = ReadBool(jsonObject, "copyToOutput", false);
            }

            return contentFile;
        }

        private static ProjectFileDependencyGroup ReadProjectFileDependencyGroup(string property, JsonValue json)
        {
            return new ProjectFileDependencyGroup(
                string.IsNullOrEmpty(property) ? null : NuGetFramework.Parse(property),
                ReadArray(json, ReadString));
        }

        private static PackageDependency ReadPackageDependency(string property, JsonValue json)
        {
            var versionStr = ReadString(json);
            return new PackageDependency(
                property,
                versionStr == null ? null : VersionRange.Parse(versionStr));
        }

        private static LockFileItem ReadFileItem(string property, JsonValue json)
        {
            var item = new LockFileItem { Path = PathUtility.GetPathWithDirectorySeparator(property) };
            var jobject = json as JsonObject;

            if (jobject != null)
            {
                foreach (var subProperty in jobject.Keys)
                {
                    item.Properties[subProperty] = jobject.ValueAsString(subProperty);
                }
            }
            return item;
        }

        private static string ReadFrameworkAssemblyReference(JsonValue json)
        {
            return ReadString(json);
        }

        private static IList<TItem> ReadArray<TItem>(JsonValue json, Func<JsonValue, TItem> readItem)
        {
            if (json == null)
            {
                return new List<TItem>();
            }

            var jarray = json as JsonArray;
            if (jarray == null)
            {
                throw FileFormatException.Create("The value type is not array.", json);
            }

            var items = new List<TItem>();
            for (int i = 0; i < jarray.Length; ++i)
            {
                items.Add(readItem(jarray[i]));
            }
            return items;
        }

        private static IList<string> ReadPathArray(JsonValue json, Func<JsonValue, string> readItem)
        {
            return ReadArray(json, readItem).Select(f => PathUtility.GetPathWithDirectorySeparator(f)).ToList();
        }

        private static IList<TItem> ReadObject<TItem>(JsonObject json, Func<string, JsonValue, TItem> readItem)
        {
            if (json == null)
            {
                return new List<TItem>();
            }
            var items = new List<TItem>();
            foreach (var childKey in json.Keys)
            {
                items.Add(readItem(childKey, json.Value(childKey)));
            }
            return items;
        }

        private static bool ReadBool(JsonObject cursor, string property, bool defaultValue)
        {
            var valueToken = cursor.Value(property) as JsonBoolean;
            if (valueToken == null)
            {
                return defaultValue;
            }

            return valueToken.Value;
        }

        private static int ReadInt(JsonObject cursor, string property, int defaultValue)
        {
            var number = cursor.Value(property) as JsonNumber;
            if (number == null)
            {
                return defaultValue;
            }

            try
            {
                var resultInInt = Convert.ToInt32(number.Raw);
                return resultInInt;
            }
            catch (Exception ex)
            {
                // FormatException or OverflowException
                throw FileFormatException.Create(ex, cursor);
            }
        }

        private static string ReadString(JsonValue json)
        {
            if (json is JsonString)
            {
                return (json as JsonString).Value;
            }
            else if (json is JsonNull)
            {
                return null;
            }
            else
            {
                throw FileFormatException.Create("The value type is not string.", json);
            }
        }
    }
}
