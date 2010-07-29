﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EnvDTE;
using Zippy.Chirp.Xml;

namespace Zippy.Chirp.Engines
{
    class ConfigEngine : Engine<ConfigEngine>
    {
        const string regularCssFile = ".css";
        const string regularJsFile = ".js";
        const string regularLessFile = ".less";

        bool IsLessFile(string fileName)
        {
            return (fileName.EndsWith(regularLessFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsCssFile(string fileName)
        {
            return (fileName.EndsWith(regularCssFile, StringComparison.OrdinalIgnoreCase));
        }

        bool IsJsFile(string fileName)
        {
            return (fileName.EndsWith(regularJsFile, StringComparison.OrdinalIgnoreCase));
        }

        public override bool IsEngineFor(string filename)
        {
            return filename.EndsWith(Settings.ChirpConfigFile, System.StringComparison.OrdinalIgnoreCase);
        }

        internal Dictionary<string, List<string>> dependentFiles =
            new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// build a dictionary that has the files that could change as the key.
        /// for the value it is a LIST of config files that need updated if it does change.
        /// so, when a .less.css file changes, we look in the list and rebuild any of the configs associated with it.
        /// if a config file changes...this rebuild all of this....
        /// </summary>
        /// <param name="projectItem"></param>
        internal void ReloadConfigFileDependencies(ProjectItem projectItem)
        {
            string configFileName = projectItem.get_FileNames(1);

            //remove all current dependencies for this config file...
            foreach (string key in dependentFiles.Keys.ToArray())
            {
                List<string> files = dependentFiles[key];
                if (files.Remove(configFileName) && files.Count == 0)
                    dependentFiles.Remove(key);
            }

            var fileGroups = LoadConfigFileGroups(configFileName);
            foreach (var fileGroup in fileGroups)
            {
                foreach (var file in fileGroup.Files)
                {
                    if (!dependentFiles.ContainsKey(file.Path))
                    {
                        dependentFiles.Add(file.Path, new List<string> { configFileName });
                    }
                    else
                    {
                        dependentFiles[file.Path].Add(configFileName);
                    }
                }
            }
        }

        IList<FileGroupXml> LoadConfigFileGroups(string configFileName)
        {
            XDocument doc = XDocument.Load(configFileName);

            string appRoot = string.Format("{0}\\", Path.GetDirectoryName(configFileName));
            return doc.Descendants("FileGroup")
                .Select(n => new FileGroupXml(n, appRoot))
                .ToList();
        }

        public override IEnumerable<IResult> Transform(EnvDTE80.DTE2 app, ProjectItem item)
        {
            string fullFileName = item.get_FileNames(1);
            var fileGroups = LoadConfigFileGroups(fullFileName);
            string directory = Path.GetDirectoryName(fullFileName);

            foreach (var fileGroup in fileGroups)
            {
                var allFileText = new StringBuilder();
                foreach (var file in fileGroup.Files)
                {
                    IEnumerable<IResult> subresult = null;
                    var path = file.Path;
                    var text = System.IO.File.ReadAllText(path);


                    if (IsLessFile(path))
                    {
                        subresult = LessEngine.Instance.Transform(path, text);

                    }
                    else if (file.Minify)
                    {
                        if (IsCssFile(path))
                        {
                            subresult = YuiCssEngine.Instance.Transform(path, text);

                        }
                        else if (IsJsFile(path))
                        {
                            subresult = YuiJsEngine.Instance.Transform(path, text);
                        }
                    }

                    if (subresult != null)
                    {
                        text = subresult.OfType<FileResult>().Where(x => x.Minified == file.Minify).Select(x => x.Text).FirstOrDefault();
                        foreach (var err in subresult.OfType<ErrorResult>()) yield return err;
                    }

                    allFileText.Append(text);
                    allFileText.Append(Environment.NewLine);
                }

                string fullPath = directory + @"\" + fileGroup.Name;

                yield return new FileResult(fullPath, allFileText.ToString(), true);
            }

            ReloadConfigFileDependencies(item);
        }

    }
}
