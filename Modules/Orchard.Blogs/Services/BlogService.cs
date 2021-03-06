using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Orchard.Blogs.Models;
using Orchard.Blogs.Routing;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Routable.Models;

namespace Orchard.Blogs.Services {
    [UsedImplicitly]
    public class BlogService : IBlogService {
        private readonly IContentManager _contentManager;
        private readonly IBlogSlugConstraint _blogSlugConstraint;

        public BlogService(IContentManager contentManager, IBlogSlugConstraint blogSlugConstraint) {
            _contentManager = contentManager;
            _blogSlugConstraint = blogSlugConstraint;
        }

        public BlogPart Get(string path) {
            return _contentManager.Query<BlogPart, BlogPartRecord>()
                .Join<RoutePartRecord>().Where(rr => rr.Path == path)
                .List().FirstOrDefault();
        }

        public ContentItem Get(int id, VersionOptions versionOptions) {
            return _contentManager.Get(id, versionOptions);
        }

        public IEnumerable<BlogPart> Get() {
            return Get(VersionOptions.Published);
        }

        public IEnumerable<BlogPart> Get(VersionOptions versionOptions) {
            return _contentManager.Query<BlogPart, BlogPartRecord>(versionOptions)
                .Join<RoutePartRecord>()
                .OrderBy(br => br.Title)
                .List();
        }

        public void Delete(ContentItem blog) {
            _contentManager.Remove(blog);
            _blogSlugConstraint.RemoveSlug(blog.As<IRoutableAspect>().Path);
        }
    }
}