﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Environment.Extensions;
using Orchard.Environment.Extensions.Models;
using Orchard.Environment.Descriptor;
using Orchard.Environment.Descriptor.Models;
using Orchard.Localization;
using Orchard.Modules.ViewModels;
using Orchard.UI.Notify;

namespace Orchard.Modules.Services {
    public interface IModuleService : IDependency {
        void EnableFeatures(IEnumerable<string> featureNames);
        void EnableFeatures(IEnumerable<string> featureNames, bool force);
        void DisableFeatures(IEnumerable<string> featureNames);
        void DisableFeatures(IEnumerable<string> featureNames, bool force);
    }

    public class ModuleService : IModuleService {
        private readonly IExtensionManager _extensionManager;
        private readonly IShellDescriptorManager _shellDescriptorManager;

        public ModuleService(
                IOrchardServices orchardServices, 
                IExtensionManager extensionManager,
                IShellDescriptorManager shellDescriptorManager) {
            Services = orchardServices;
            _extensionManager = extensionManager;
            _shellDescriptorManager = shellDescriptorManager;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public IOrchardServices Services { get; set; }

        //public IModule GetModuleByName(string moduleName) {
        //    return _extensionManager
        //        .AvailableExtensions()
        //        .Where(e => string.Equals(e.Name, moduleName, StringComparison.OrdinalIgnoreCase))
        //        .Where(e => string.Equals(e.ExtensionType, ModuleExtensionType, StringComparison.OrdinalIgnoreCase))
        //        .Select(descriptor => AssembleModuleFromDescriptor(descriptor))
        //        .FirstOrDefault();
        //}

        public IEnumerable<ModuleFeature> GetAvailableFeatures() {
            var enabledFeatures = _shellDescriptorManager.GetShellDescriptor().Features;
            return _extensionManager.AvailableExtensions()
                .SelectMany(m => _extensionManager.LoadFeatures(m.Features))
                .Select(f => AssembleModuleFromDescriptor(f, enabledFeatures
                    .FirstOrDefault(sf => string.Equals(sf.Name, f.Descriptor.Id, StringComparison.OrdinalIgnoreCase)) != null));
        }

        public void EnableFeatures(IEnumerable<string> featureNames) {
            EnableFeatures(featureNames, false);
        }

        public void EnableFeatures(IEnumerable<string> features, bool force) {
            var shellDescriptor = _shellDescriptorManager.GetShellDescriptor();
            var enabledFeatures = shellDescriptor.Features.ToList();
            var availableFeatures = GetAvailableFeatures().ToList();

            var featuresToEnable = features
                .Select(s => EnableFeature(s, availableFeatures, force)).ToList()
                .SelectMany(ies => ies.Select(s => s));

            if (featuresToEnable.Count() == 0)
                return;

            foreach (var featureToEnable in featuresToEnable) {
                var feature = featureToEnable; 
                enabledFeatures.Add(new ShellFeature { Name = feature });
                Services.Notifier.Information(T("{0} was enabled", featureToEnable));
            }

            _shellDescriptorManager.UpdateShellDescriptor(shellDescriptor.SerialNumber, enabledFeatures,
                                                          shellDescriptor.Parameters);
        }

        public void DisableFeatures(IEnumerable<string> featureNames) {
            DisableFeatures(featureNames, false);
        }

        public void DisableFeatures(IEnumerable<string> features, bool force) {
            var shellDescriptor = _shellDescriptorManager.GetShellDescriptor();
            var enabledFeatures = shellDescriptor.Features.ToList();
            var availableFeatures = GetAvailableFeatures().ToList();

            var featuresToDisable = features
                .Select(s => DisableFeature(s, availableFeatures, force)).ToList()
                .SelectMany(ies => ies.Select(s => s));

            if (featuresToDisable.Count() == 0)
                return;

            foreach (var featureToDisable in featuresToDisable) {
                var feature = featureToDisable;
                enabledFeatures.RemoveAll(f => f.Name == feature);
                Services.Notifier.Information(T("{0} was disabled", feature));
            }

            _shellDescriptorManager.UpdateShellDescriptor(shellDescriptor.SerialNumber, enabledFeatures,
                                                          shellDescriptor.Parameters);
        }

        private IEnumerable<string> EnableFeature(string featureName, IEnumerable<ModuleFeature> features, bool force) {
            var featuresList = features.ToList();
            var getDisabledDependencies =
                new Func<string, IEnumerable<ModuleFeature>, IEnumerable<ModuleFeature>>(
                    (n, fs) => {
                        var feature = fs.Single(f => f.Descriptor.Id == n);
                        return feature.Descriptor.Dependencies != null
                                   ? feature.Descriptor.Dependencies.Select(
                                       fn => fs.Single(f => f.Descriptor.Id == fn)).Where(f => !f.IsEnabled)
                                   : Enumerable.Empty<ModuleFeature>();
                    });

            var featuresToEnable = GetAffectedFeatures(featureName, featuresList, getDisabledDependencies);

            if (featuresToEnable.Count() > 1 && !force) {
                GenerateWarning("If you want {0} enabled, then you'll also need to enable {1}.",
                                featureName,
                                featuresToEnable.Where(fn => fn != featureName));
                return Enumerable.Empty<string>();
            }

            return featuresToEnable;
        }

        private IEnumerable<string> DisableFeature(string featureName, IEnumerable<ModuleFeature> features, bool force) {
            var featuresList = features.ToList();
            var getEnabledDependants =
                new Func<string, IEnumerable<ModuleFeature>, IEnumerable<ModuleFeature>>(
                    (n, fs) => fs.Where(f => f.IsEnabled && f.Descriptor.Dependencies != null && f.Descriptor.Dependencies.Contains(n)));

            var featuresToDisable = GetAffectedFeatures(featureName, featuresList, getEnabledDependants);

            if (featuresToDisable.Count() > 1 && !force) {
                GenerateWarning("If {0} is disabled, then you'll also lose {1}.",
                                featureName,
                                featuresToDisable.Where(fn => fn != featureName));
                return Enumerable.Empty<string>();
            }

            return featuresToDisable;
        }

        private static IEnumerable<string> GetAffectedFeatures(string featureName, IEnumerable<ModuleFeature> features, Func<string, IEnumerable<ModuleFeature>, IEnumerable<ModuleFeature>> getAffectedDependencies) {
            var dependencies = new List<string> {featureName};

            foreach (var dependency in getAffectedDependencies(featureName, features))
                dependencies.AddRange(GetAffectedFeatures(dependency.Descriptor.Id, features, getAffectedDependencies));

            return dependencies;
        }

        private void GenerateWarning(string messageFormat, string featureName, IEnumerable<string> featuresInQuestion) {
            if (featuresInQuestion.Count() < 1)
                return;

            Services.Notifier.Warning(T(
                messageFormat,
                featureName,
                featuresInQuestion.Count() > 1
                    ? string.Join("",
                                  featuresInQuestion.Select(
                                      (fn, i) =>
                                      T(i == featuresInQuestion.Count() - 1
                                            ? "{0}"
                                            : (i == featuresInQuestion.Count() - 2
                                                   ? "{0} and "
                                                   : "{0}, "), fn).ToString()).ToArray())
                    : featuresInQuestion.First()));
        }

        private static ModuleFeature AssembleModuleFromDescriptor(Feature feature, bool isEnabled) {
            return new ModuleFeature {
                                         Descriptor = feature.Descriptor,
                                         IsEnabled = isEnabled
                                     };
        }
    }
}