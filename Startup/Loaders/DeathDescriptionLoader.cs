using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using XRL;

namespace UD_FleshGolems.Startup.Loaders
{
    public class DeathDescriptionLoader
    {
        public const string ROOT_NAME = "deaths";
        public const string NODE_NAME = "death";

        public class DeathDescriptionXMLData
        {
            public string Name;
            public string Inherits;
            public string Load;

            public string Category;
            public bool? Were;
            public string Killed;
            public string Killing;
            public bool? By;
            public string Killer;
            public bool? With;
            public string Method;
            public bool? ForceNoMethodArticle;
            public bool? PluralMethod;

            public Dictionary<string, string> Attributes;

            public ModInfo Mod;

            public DeathDescriptionXMLData()
            {
                Name = null;
                Inherits = null;
                Load = null;

                Category = null;
                Were = null;
                Killed = null;
                Killing = null;
                By = null;
                Killer = null;
                With = null;
                Method = null;
                ForceNoMethodArticle = null;
                PluralMethod = null;

                Attributes = null;

                Mod = null;
            }

            public string ReadAttribute(XmlTextReader Reader, ref string Value)
            {
                Attributes ??= new();

                switch (Reader.Name)
                {
                    case nameof(Name):
                        Name = Reader.Value;
                        break;
                    case nameof(Inherits):
                        Inherits = Reader.Value;
                        break;
                    case nameof(Load):
                        Load = Reader.Value;
                        break;

                    case nameof(Category):
                        Category = Reader.Value;
                        break;
                    case nameof(Were):
                        Were = Reader.Value?.EqualsNoCase("true");
                        break;
                    case nameof(Killed):
                        Killed = Reader.Value;
                        break;
                    case nameof(Killing):
                        Killing = Reader.Value;
                        break;
                    case nameof(By):
                        By = Reader.Value?.EqualsNoCase("true");
                        break;
                    case nameof(Killer):
                        Killer = Reader.Value;
                        break;
                    case nameof(With):
                        With = Reader.Value?.EqualsNoCase("true");
                        break;
                    case nameof(Method):
                        Method = Reader.Value;
                        break;
                    case nameof(ForceNoMethodArticle):
                        ForceNoMethodArticle = Reader.Value?.EqualsNoCase("true");
                        break;
                    case nameof(PluralMethod):
                        PluralMethod = Reader.Value?.EqualsNoCase("true");
                        break;

                    default:
                        Attributes[Reader.Name] = Reader.Value;
                        break;
                }
                return Value;
            }

            public static DeathDescriptionXMLData ReadDeathNode(XmlTextReader Reader, ModInfo Mod = null)
            {
                if (Reader == null)
                {
                    handleError("Null " + nameof(Reader));
                    return new();
                }
                string xmlFilePath = DataManager.SanitizePathForDisplay(Reader.BaseURI);
                DeathDescriptionXMLData deathDescriptionXMLData = new()
                {
                    Attributes = new(),
                    Mod = Mod,
                };
                if (Reader.HasAttributes)
                {
                    while (Reader.MoveToNextAttribute())
                    {
                        
                    }
                }
                if (Reader.NodeType == XmlNodeType.EndElement || Reader.IsEmptyElement)
                {
                    return deathDescriptionXMLData;
                }
                while (Reader.Read())
                {
                    if (Reader.NodeType == XmlNodeType.Comment || Reader.NodeType == XmlNodeType.Text)
                    {
                        continue;
                    }
                    if (Reader.NodeType == XmlNodeType.EndElement && Reader.Name == NODE_NAME)
                    {
                        return deathDescriptionXMLData;
                    }
                    if (Reader.NodeType == XmlNodeType.Element)
                    {
                        handleError(xmlFilePath + ": Unknown " + NODE_NAME + " element " + Reader.Name + " at line " + Reader.LineNumber);
                    }
                    else
                    {
                        handleError(xmlFilePath + ": Unknown problem reading object: " + Reader.NodeType);
                    }
                }
                return deathDescriptionXMLData;
            }
        }

        protected static Action<object> handleError;
        protected static Action<object> handleWarning;

        public void LoadAllDeathDescriptions()
        {
            foreach (XmlDataHelper dataHelper in DataManager.YieldXMLStreamsWithRoot(ROOT_NAME))
            {
                if (dataHelper.modInfo != null)
                {
                    handleError = dataHelper.modInfo.Error;
                    handleWarning = dataHelper.modInfo.Warn;
                }
                else
                {
                    handleError = MetricsManager.LogError;
                    handleWarning = MetricsManager.LogWarning;
                }
                try
                {
                    ReadDeathDescriptionsXML(dataHelper, dataHelper.modInfo);
                }
                catch (Exception x)
                {
                    MetricsManager.LogPotentialModError(dataHelper.modInfo, x);
                }
            }
        }

        private static string FileLinePosition(string File, int Line, int Position)
            => "File: " + File + ", Line: " + Line + ":" + Position;

        private static string FileLinePosition(XmlTextReader reader)
            => FileLinePosition(DataManager.SanitizePathForDisplay(reader.BaseURI), reader.LineNumber, reader.LinePosition);

        private static Exception NewFileLinePositionException(XmlTextReader reader, Exception InnerException)
            => new(FileLinePosition(reader), InnerException);

        public void ReadDeathDescriptionsXML(XmlTextReader reader, ModInfo modInfo = null)
        {
            string xmlFileName = DataManager.SanitizePathForDisplay(reader.BaseURI);
            handleError ??= MetricsManager.LogError;
            bool rootExists = false;
            try
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;
                while (reader.Read())
                {
                    if (reader.Name == ROOT_NAME)
                    {
                        rootExists = true;
                        if (MarkovCorpusGenerator.Generating
                            && reader.GetAttribute("ExcludeFromCorpusGeneration").EqualsNoCase("true"))
                        {
                            return;
                        }
                        ReadDeathsNode(reader, modInfo);
                    }
                }
            }
            catch (Exception innerException)
            {
                throw NewFileLinePositionException(reader, innerException);
            }
            finally
            {
                reader.Close();
            }
            if (!rootExists)
            {
                handleError("No <" + ROOT_NAME + "> tag found in " + xmlFileName);
            }
        }

        
        public int ReadDeathsNode(XmlTextReader reader, ModInfo modInfo = null)
        {
            int num = 0;
            /*
            while (reader.Read())
            {
                if (reader.Name == NODE_NAME)
                {
                    DeathDescriptionXMLData objectBlueprintXMLData = DeathDescriptionXMLData.ReadDeathNode(reader);
                    objectBlueprintXMLData.Mod = modInfo;
                    num++;
                    if (objectBlueprintXMLData.Load == "Merge")
                    {
                        string text = objectBlueprintXMLData.Name;
                        if (CompatManager.TryGetCompatEntry("blueprint", text, out var NewID))
                        {
                            handleWarning($"File: {DataManager.SanitizePathForDisplay(reader.BaseURI)}, Line: {reader.LineNumber}:{reader.LinePosition} Attempt to merge with {text} which has a new name \"{NewID}\"");
                            text = NewID;
                        }
                        if (!Objects.TryGetValue(text, out var value))
                        {
                            handleError($"File: {DataManager.SanitizePathForDisplay(reader.BaseURI)}, Line: {reader.LineNumber}:{reader.LinePosition} Attempt to merge with {text} which is an unknown blueprint, node discarded");
                        }
                        else
                        {
                            value.Merge(objectBlueprintXMLData);
                        }
                    }
                    else if (objectBlueprintXMLData.Load == "MergeIfExists")
                    {
                        string text2 = objectBlueprintXMLData.Name;
                        if (CompatManager.TryGetCompatEntry("blueprint", text2, out var NewID2))
                        {
                            handleWarning($"File: {DataManager.SanitizePathForDisplay(reader.BaseURI)}, Line: {reader.LineNumber}:{reader.LinePosition} Attempt to merge with {text2} which has a new name \"{NewID2}\"");
                            text2 = NewID2;
                        }
                        if (Objects.TryGetValue(text2, out var value2))
                        {
                            value2.Merge(objectBlueprintXMLData);
                        }
                    }
                    else
                    {
                        Objects[objectBlueprintXMLData.Name] = objectBlueprintXMLData;
                    }
                }
                else if (reader.NodeType != XmlNodeType.Comment)
                {
                    if (reader.Name == "objects" && reader.NodeType == XmlNodeType.EndElement)
                    {
                        return num;
                    }
                    throw new Exception("Unknown node '" + reader.Name + "'");
                }
            }
            */
            return num;
        }
    }
}
