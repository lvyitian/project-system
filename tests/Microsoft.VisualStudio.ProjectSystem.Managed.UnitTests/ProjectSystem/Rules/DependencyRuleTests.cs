﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class DependencyRuleTests : XamlRuleTestBase
    {
        [Theory]
        [MemberData(nameof(GetResolvedDependenciesRules))]
        public void VisibleEditableResolvedDependenciesMustHaveDataSource(string ruleName, string fullPath)
        {
            // Resolved rules get their data from design time targets. Any editable properties need a
            // property-level data source that specifies the storage for that property as the project file
            // so that changes made in the properties pane are reflected in the project file and vice versa.

            XElement rule = LoadXamlRule(fullPath);

            var itemType = rule
                .Element(XName.Get("Rule.DataSource", MSBuildNamespace))
                ?.Element(XName.Get("DataSource", MSBuildNamespace))
                ?.Attribute("ItemType")?.Value;

            Assert.NotNull(itemType);

            foreach (var property in GetProperties(rule))
            {
                // Properties are visible and non-readonly by default
                string visibleValue = property.Attribute("Visible")?.Value ?? "true";
                string readOnlyValue = property.Attribute("ReadOnly")?.Value ?? "false";

                Assert.True(bool.TryParse(visibleValue, out bool visible));
                Assert.True(bool.TryParse(readOnlyValue, out bool readOnly));

                if (!visible || readOnly)
                {
                    continue;
                }

                var dataSourceElementName = $"{property.Name.LocalName}.DataSource";

                var dataSource = property
                    .Element(XName.Get(dataSourceElementName, MSBuildNamespace))
                    ?.Element(XName.Get("DataSource", MSBuildNamespace));

                if (dataSource == null)
                {
                    throw new Xunit.Sdk.XunitException($"Resolved dependency rule {ruleName} has visible, non-readonly property {property.Attribute("Name")} with no {dataSourceElementName} value.");
                }

                Assert.Equal("False",        dataSource.Attribute("HasConfigurationCondition")?.Value, StringComparer.OrdinalIgnoreCase);
                Assert.Equal("ProjectFile",  dataSource.Attribute("Persistence")?.Value,               StringComparer.Ordinal);
                Assert.Equal("AfterContext", dataSource.Attribute("SourceOfDefaultValue")?.Value,      StringComparer.Ordinal);
                Assert.Equal(itemType,       dataSource.Attribute("ItemType")?.Value,                  StringComparer.Ordinal);
            }
        }

        [Theory]
        [MemberData(nameof(GetResolvedDependenciesRules))]
        public void ResolvedDependenciesRulesMustHaveOriginalItemSpecProperty(string ruleName, string fullPath)
        {
            // All resolved dependency items have a corresponding 'original' item spec, which contains
            // the value of the item produced by evaluation.

            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:StringProperty[@Name=""OriginalItemSpec""]", namespaceManager);

            Assert.NotNull(property);
            Assert.Equal(3, property.Attributes().Count());
            Assert.Equal("OriginalItemSpec", property.Attribute("Name")?.Value, StringComparer.Ordinal);
            Assert.Equal("False", property.Attribute("Visible")?.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("True", property.Attribute("ReadOnly")?.Value, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        public void DependenciesRulesMustHaveVisibleProperty(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:BoolProperty[@Name=""Visible""]", namespaceManager);

            Assert.NotNull(property);
            Assert.Equal(3, property.Attributes().Count());
            Assert.Equal("Visible", property.Attribute("Name")?.Value, StringComparer.Ordinal);
            Assert.Equal("False", property.Attribute("Visible")?.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("True", property.Attribute("ReadOnly")?.Value, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        public void DependenciesRulesMustHaveIsImplicitlyDefinedProperty(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            var property = rule.XPathSelectElement(@"/msb:Rule/msb:StringProperty[@Name=""IsImplicitlyDefined""]", namespaceManager);

            Assert.NotNull(property);
            Assert.Equal(3, property.Attributes().Count());
            Assert.Equal("IsImplicitlyDefined", property.Attribute("Name")?.Value, StringComparer.Ordinal);
            Assert.Equal("False", property.Attribute("Visible")?.Value, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("True", property.Attribute("ReadOnly")?.Value, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        public void DependenciesRulesDescriptionHaveCorrectSuffix(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            Assert.EndsWith(" Reference Properties", rule.Attribute("Description")?.Value);
        }

        [Theory]
        [MemberData(nameof(GetDependenciesRules))]
        public void DependenciesRulesDisplayNameHaveCorrectSuffix(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath);

            Assert.EndsWith(" Reference", rule.Attribute("DisplayName")?.Value);
        }

        [Theory]
        [MemberData(nameof(GetUnresolvedDependenciesRules))]
        public void UnresolvedDependenciesRulesMustNotHaveOriginalItemSpec(string ruleName, string fullPath)
        {
            XElement rule = LoadXamlRule(fullPath, out var namespaceManager);

            XElement? property = rule.XPathSelectElement(@"/msb:Rule/msb:StringProperty[@Name=""OriginalItemSpec""]", namespaceManager);

            Assert.Null(property);
        }

        [Theory]
        [MemberData(nameof(GetResolvedAndUnresolvedDependencyRulePairs))]
        public void ResolvedAndUnresolvedDependencyRulesHaveSameDisplayName(string unresolvedName, string resolvedName, string unresolvedPath, string resolvedPath)
        {
            XElement unresolvedRule = LoadXamlRule(unresolvedPath);
            XElement resolvedRule = LoadXamlRule(resolvedPath);

            Assert.Equal(unresolvedRule.Attribute("DisplayName")?.Value, resolvedRule.Attribute("DisplayName")?.Value);
        }

        [Theory]
        [MemberData(nameof(GetResolvedAndUnresolvedDependencyRulePairs))]
        public void ResolvedAndUnresolvedDependencyRulesHaveSameDescription(string unresolvedName, string resolvedName, string unresolvedPath, string resolvedPath)
        {
            XElement unresolvedRule = LoadXamlRule(unresolvedPath);
            XElement resolvedRule = LoadXamlRule(resolvedPath);

            Assert.Equal(unresolvedRule.Attribute("Description")?.Value, resolvedRule.Attribute("Description")?.Value);
        }

        [Theory]
        [MemberData(nameof(GetResolvedAndUnresolvedDependencyRulePairs))]
        public void ResolvedAndUnresolvedDependencyRulesHaveSameVisibleProperties(string unresolvedName, string resolvedName, string unresolvedPath, string resolvedPath)
        {
            Dictionary<string, XElement> unresolvedPropertyByName = GetVisibleProperties(LoadXamlRule(unresolvedPath)).ToDictionary(prop => prop.Attribute("Name").Value);
            Dictionary<string, XElement> resolvedPropertyByName = GetVisibleProperties(LoadXamlRule(resolvedPath)).ToDictionary(prop => prop.Attribute("Name").Value);

            var missingInUnresolved = resolvedPropertyByName.Keys.Except(unresolvedPropertyByName.Keys).ToList();
            var missingInResolved = unresolvedPropertyByName.Keys.Except(resolvedPropertyByName.Keys).ToList();

            Assert.True(missingInUnresolved.Count == 0, "Resolved properties not found in unresolved: " + string.Join(", ", missingInUnresolved));
            Assert.True(missingInResolved.Count == 0, "Unresolved properties not found in resolved: " + string.Join(", ", missingInResolved));
            Assert.Equal(unresolvedPropertyByName.Count, resolvedPropertyByName.Count); // should be redundant given the above two checks

            foreach ((string name, XElement resolved) in resolvedPropertyByName)
            {
                XElement unresolved = unresolvedPropertyByName[name];

                Assert.Equal(resolved.Attribute("Description")?.Value, unresolved.Attribute("Description")?.Value);
                Assert.Equal(resolved.Attribute("DisplayName")?.Value, unresolved.Attribute("DisplayName")?.Value);
                Assert.True(string.Equals(resolved.Attribute("ReadOnly")?.Value, unresolved.Attribute("ReadOnly")?.Value),
                    $"ReadOnly attribute for property {name} differs between unresolved/resolved rules");
            }
        }

        public static IEnumerable<object[]> GetUnresolvedDependenciesRules()
        {
            return Project(GetRules("Dependencies")
                .Where(fileName => fileName.IndexOf("Resolved", StringComparisons.Paths) == -1));
        }

        public static IEnumerable<object[]> GetResolvedDependenciesRules()
        {
            return Project(GetRules("Dependencies")
                .Where(fileName => fileName.IndexOf("Resolved", StringComparisons.Paths) != -1));
        }

        public static IEnumerable<object[]> GetDependenciesRules()
        {
            return Project(GetRules("Dependencies"));
        }

        public static IEnumerable<object[]> GetResolvedAndUnresolvedDependencyRulePairs()
        {
            List<string> rules = GetRules("Dependencies").ToList();

            HashSet<string> unresolvedPaths = rules.Where(fileName => fileName.IndexOf("Resolved", StringComparisons.Paths) == -1).ToHashSet(StringComparer.Ordinal);
            HashSet<string> resolvedPaths = rules.Where(fileName => fileName.IndexOf("Resolved", StringComparisons.Paths) != -1).ToHashSet(StringComparer.Ordinal);

            Assert.Equal(resolvedPaths.Count, unresolvedPaths.Count);

            foreach (string unresolvedPath in unresolvedPaths)
            {
                string unresolvedName = Path.GetFileNameWithoutExtension(unresolvedPath);
                string resolvedName = "Resolved" + unresolvedName;
                string resolvedPath = Path.Combine(Path.GetDirectoryName(unresolvedPath), resolvedName + ".xaml");

                Assert.Contains(resolvedPath, resolvedPaths);

                yield return new object[] { unresolvedName, resolvedName, unresolvedPath, resolvedPath };
            }
        }
    }
}
