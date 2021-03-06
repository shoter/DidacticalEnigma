﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DidacticalEnigma.Core.Models.Project
{
    public class RWLazy<T> : IDisposable
        where T : IDisposable
    {
        private T value = default;

        private readonly Func<T> factory;

        public bool IsValueCreated { get; private set; } = false;

        public T Value
        {
            get
            {
                if (IsValueCreated)
                    return value;

                value = factory();
                IsValueCreated = true;
                return value;
            }
            set
            {
                this.value = value;
                IsValueCreated = true;
            }
        }

        public RWLazy(Func<T> factory)
        {
            this.factory = factory;
        }

        public void Dispose()
        {
            if (IsValueCreated)
            {
                value.Dispose();
                IsValueCreated = false;
            }
        }
    }

    public class Project : IDisposable
    {
        public string RootPath => Path.Combine(ProjectPath, "..");

        public string ProjectPath { get; }

        public IDictionary<string, RWLazy<File>> Files { get; }

        private ProjectData Data { get; }

        private class ProjectData
        {
            public string Version { get; }

            public IReadOnlyCollection<string> Files { get; set; }

            [JsonExtensionData]
            private readonly IDictionary<string, JToken> additionalData;

            public ProjectData(string version, IReadOnlyCollection<string> files)
            {
                Version = version;
                Files = files;
            }
        }

        public static Project Open(string projectPath)
        {
            var projectData = JsonConvert.DeserializeObject<ProjectData>(
                System.IO.File.ReadAllText(Path.Combine(projectPath, "project.de"), Encoding.UTF8));

            var files = projectData.Files
                .ToDictionary(path => path, path => new RWLazy<File>(() => new File(Path.Combine(projectPath, "..", path))));

            return new Project(projectPath, files, projectData);
        }

        public static Project CreateNew(string projectPath)
        {
            var projectData = new ProjectData("1.0", new List<string>());

            var files = new Dictionary<string, RWLazy<File>>();

            return new Project(projectPath, files, projectData);
        }

        private Project(string path, IDictionary<string, RWLazy<File>> files, ProjectData projectData)
        {
            ProjectPath = path;
            Files = files;
            Data = projectData;
        }

        public class File : IDisposable
        {
            public string Path { get; }

            public IList<Translation> Translations { get; }

            public File(string path)
            {
                Path = path;
                try
                {
                    Translations = JsonConvert.DeserializeObject<List<Translation>>(System.IO.File.ReadAllText(path, Encoding.UTF8));
                }
                catch (FileNotFoundException)
                {
                    Translations = new List<Translation>();
                }
            }

            public void Save()
            {
                System.IO.File.WriteAllText(Path, JsonConvert.SerializeObject(Translations), Encoding.UTF8);
            }

            public void Dispose()
            {
                Save();
            }
        }

        public void Save()
        {
            foreach (var file in Files)
            {
                file.Value.Dispose();
            }

            Data.Files = Files.Select(kvp => kvp.Key).ToList();

            System.IO.File.WriteAllText(
                Path.Combine(RootPath, "project.de"),
                JsonConvert.SerializeObject(Data),
                Encoding.UTF8);
        }

        public void Dispose()
        {
            Save();
        }
    }

    public class Translation
    {
        public Guid Guid { get; }

        private string originalText;

        public string OriginalText
        {
            get => originalText;
            set => originalText = value ?? throw new ArgumentNullException(nameof(value));
        }

        // Can be null
        public string TranslatedText { get; set; }

        public IList<GlossNote> Glosses { get; }

        public IList<TranslatorNote> Notes { get; }

        public IList<TranslatedText> AlternativeTranslations { get; }

        public Translation(
            Guid guid,
            string originalText,
            string translatedText = null,
            IList<GlossNote> glosses = null,
            IList<TranslatorNote> notes = null,
            IList<TranslatedText> alternativeTranslations = null)
        {
            Guid = guid;
            OriginalText = originalText;
            TranslatedText = translatedText;
            Glosses = (glosses ?? Enumerable.Empty<GlossNote>()).ToList();
            Notes = (notes ?? Enumerable.Empty<TranslatorNote>()).ToList();
            AlternativeTranslations = (alternativeTranslations ?? Enumerable.Empty<TranslatedText>()).ToList();
        }

        public static Translation CreateNew(string originalText, string translatedText)
        {
            return new Translation(System.Guid.NewGuid(), originalText);
        }

        [JsonExtensionData]
        private readonly IDictionary<string, JToken> additionalData;
    }

    public class TranslatedText
    {
        public string Text { get; }

        public string Author { get; }

        public TranslatedText(string author, string text)
        {
            Author = author;
            Text = text;
        }

        [JsonExtensionData]
        private readonly IDictionary<string, JToken> additionalData;
    }

    public class TranslatorNote : IEquatable<TranslatorNote>
    {
        public bool Equals(TranslatorNote other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Text, other.Text);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TranslatorNote) obj);
        }

        public override int GetHashCode()
        {
            return (Text != null ? Text.GetHashCode() : 0);
        }

        public static bool operator ==(TranslatorNote left, TranslatorNote right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TranslatorNote left, TranslatorNote right)
        {
            return !Equals(left, right);
        }

        public string Text { get; }

        public TranslatorNote(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"{Text}";
        }
    }

    public class GlossNote : TranslatorNote, IEquatable<GlossNote>
    {
        public bool Equals(GlossNote other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && string.Equals(Foreign, other.Foreign);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GlossNote) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Foreign != null ? Foreign.GetHashCode() : 0);
            }
        }

        public static bool operator ==(GlossNote left, GlossNote right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GlossNote left, GlossNote right)
        {
            return !Equals(left, right);
        }

        public string Foreign { get; }

        public GlossNote(string foreign, string text) :
            base(text)
        {
            Foreign = foreign ?? throw new ArgumentNullException(nameof(foreign));
        }

        public override string ToString()
        {
            return $"{Foreign}: {Text}";
        }
    }
}
