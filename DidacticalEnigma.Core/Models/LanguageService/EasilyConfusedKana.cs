﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DidacticalEnigma.Core.Utils;

namespace DidacticalEnigma.Core.Models.LanguageService
{
    public class EasilyConfusedKana : IRelated
    {
        private Dictionary<CodePoint, List<CodePoint>> similarityGroups;

        private static readonly IReadOnlyDictionary<Type, int> position = new Dictionary<Type, int>
        {
            { typeof(Hiragana), 1 },
            { typeof(Katakana), 2 },
            { typeof(Kanji), 4 },
            { typeof(CodePoint), 8 },
        };

        private IEnumerable<CodePoint> FindSimilar(CodePoint point)
        {
            similarityGroups.TryGetValue(point, out var listOfSimilar);
            var similar = listOfSimilar ?? Enumerable.Empty<CodePoint>();
            return similar
                .Except(Enumerable.Repeat(point, 1))
                .OrderBy(other =>
                {
                    return Math.Abs(position[point.GetType()] - position[other.GetType()]);
                });
        }

        private EasilyConfusedKana(IEnumerable<IEnumerable<CodePoint>> input)
        {
            var similaritySets = new ConcurrentDictionary<CodePoint, UnionFindNode>();
            foreach (var group in input)
            {
                UnionFindNode first = null;
                foreach (var cp in group)
                {
                    var set = similaritySets.GetOrAdd(cp, x => new UnionFindNode());
                    if(first != null)
                    {
                        set.Union(first);
                    }
                    first = set;
                }
            }
            var uniqueLists = new Dictionary<UnionFindNode, List<CodePoint>>();
            foreach(var kvp in similaritySets)
            {
                uniqueLists[kvp.Value.Find()] = new List<CodePoint>();
            }
            similarityGroups = new Dictionary<CodePoint, List<CodePoint>>();
            foreach(var kvp in similaritySets)
            {
                var list = uniqueLists[similaritySets[kvp.Key].Find()];
                list.Add(kvp.Key);
                similarityGroups[kvp.Key] = list;
            }
        }

        public static EasilyConfusedKana FromFile(string path)
        {
            return new EasilyConfusedKana(
                File.ReadLines(path, Encoding.UTF8)
                    .Where(line => !line.StartsWith("#"))
                    .Select(line => line.AsCodePoints().Select(cp => CodePoint.FromInt(cp))));
            
        }

        public IEnumerable<IGrouping<string, CodePoint>> FindRelated(CodePoint codePoint)
        {
            return new IGrouping<string, CodePoint>[]
            {
                new CategoryGrouping("Similarly looking", FindSimilar(codePoint))
            };
        }
    }
}
