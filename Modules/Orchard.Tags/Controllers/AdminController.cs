﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Mvc.Extensions;
using Orchard.Tags.Models;
using Orchard.Tags.ViewModels;
using Orchard.Tags.Services;

namespace Orchard.Tags.Controllers {
    [ValidateInput(false)]
    public class AdminController : Controller {
        private readonly ITagService _tagService;

        public AdminController(IOrchardServices services, ITagService tagService) {
            Services = services;
            _tagService = tagService;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        
        public Localizer T { get; set; }

        public ActionResult Index() {
            IEnumerable<TagRecord> tags = _tagService.GetTags();
            var entries = tags.Select(CreateTagEntry).ToList();
            var model = new TagsAdminIndexViewModel { Tags = entries };
            return View(model);
        }

        [HttpPost]
        [FormValueRequired("submit.BulkEdit")]
        public ActionResult Index(FormCollection input)
        {
            var viewModel = new TagsAdminIndexViewModel {Tags = new List<TagEntry>(), BulkAction = new TagAdminIndexBulkAction()};
            
            if ( !TryUpdateModel(viewModel) ) {
                return View(viewModel);
            }

            IEnumerable<TagEntry> checkedEntries = viewModel.Tags.Where(t => t.IsChecked);
            switch (viewModel.BulkAction) {
                case TagAdminIndexBulkAction.None:
                    break;
                case TagAdminIndexBulkAction.Delete:
                    if (!Services.Authorizer.Authorize(Permissions.ManageTags, T("Couldn't delete tag")))
                        return new HttpUnauthorizedResult();

                    foreach (TagEntry entry in checkedEntries) {
                        _tagService.DeleteTag(entry.Tag.Id);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Index")]
        [FormValueRequired("submit.Create")]
        public ActionResult IndexCreatePOST() {
            var viewModel = new TagsAdminCreateViewModel();

            if (!TryUpdateModel(viewModel)) {
                ViewData["CreateTag"] = viewModel;
                return Index();
            }
            
            _tagService.CreateTag(viewModel.TagName);
            
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id) {
            TagRecord tagRecord = _tagService.GetTag(id);

            if(tagRecord == null) {
                return RedirectToAction("Index");
            }

            var viewModel = new TagsAdminEditViewModel {
                Id = tagRecord.Id,
                TagName = tagRecord.TagName,
            };

            ViewData["ContentItems"] = _tagService.GetTaggedContentItems(id, VersionOptions.Latest).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Edit(FormCollection input) {
            var viewModel = new TagsAdminEditViewModel();

            if ( !TryUpdateModel(viewModel) ) {
                return View(viewModel);
            }
            
            if (!Services.Authorizer.Authorize(Permissions.ManageTags, T("Couldn't edit tag")))
                return new HttpUnauthorizedResult();

            _tagService.UpdateTag(viewModel.Id, viewModel.TagName);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Remove(int id, string returnUrl) {
            if (!Services.Authorizer.Authorize(Permissions.ManageTags, T("Couldn't remove tag")))
                return new HttpUnauthorizedResult();

            TagRecord tagRecord = _tagService.GetTag(id);

            if (tagRecord == null)
                return new HttpNotFoundResult();

            _tagService.DeleteTag(id);

            return this.RedirectLocal(returnUrl, () => RedirectToAction("Index"));
        }

        private static TagEntry CreateTagEntry(TagRecord tagRecord) {
            return new TagEntry {
                Tag = tagRecord,
                IsChecked = false,
            };
        }

        public class FormValueRequiredAttribute : ActionMethodSelectorAttribute {
            private readonly string _submitButtonName;

            public FormValueRequiredAttribute(string submitButtonName) {
                _submitButtonName = submitButtonName;
            }

            public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo) {
                var value = controllerContext.HttpContext.Request.Form[_submitButtonName];
                return !string.IsNullOrEmpty(value);
            }
        }
    }
}
