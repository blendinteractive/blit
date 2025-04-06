using System.Reflection.Emit;

namespace BlendInteractive.Blit.Builders;

public class ContentBuilder
{
    private readonly string id;
    private readonly string type;
    private readonly ContentActions actions;

    public ContentBuilder(string id, string type, ContentActions actions)
    {
        this.id = id;
        this.type = type;
        this.actions = actions;
    }

    public ContentBuilderStep2 Query(Action<ContentQueryBuilder> action)
    {
        var builder = new ContentQueryBuilder();
        action(builder);
        return new ContentBuilderStep2(id, type, actions, new ContentQuery(builder.Done()));
    }

    public class ContentBuilderStep2
    {
        private readonly string id;
        private readonly string type;
        private readonly ContentActions actions;
        private readonly ContentQuery query;

        public ContentBuilderStep2(string id, string type, ContentActions actions, ContentQuery query)
        {
            this.id = id;
            this.type = type;
            this.actions = actions;
            this.query = query;
        }

        public ContentBuilderStep3 SkipParentQuery()
            => new ContentBuilderStep3(id, type, actions, query, null);

        public ContentBuilderStep3 ParentQuery(Action<ContentQueryBuilder> action)
        {
            var builder = new ContentQueryBuilder();
            action(builder);
            return new ContentBuilderStep3(id, type, actions, query, new ContentQuery(builder.Done()));
        }
    }

    public class ContentBuilderStep3
    {
        private readonly string id;
        private readonly string type;
        private readonly ContentActions actions;
        private readonly ContentQuery query;
        private readonly ContentQuery? parent;

        public ContentBuilderStep3(string id, string type, ContentActions actions, ContentQuery query, ContentQuery? parent)
        {
            this.id = id;
            this.type = type;
            this.actions = actions;
            this.query = query;
            this.parent = parent;
        }

        public ContentBuilderStep4 StageOneProperties(Action<PropertyBuilder> action)
        {
            var builder = new PropertyBuilder();
            action(builder);
            return new ContentBuilderStep4(id, type, actions, query, parent, builder.Done());
        }

        public ContentBuilderStep4 SkipStageOne() => new ContentBuilderStep4(id, type, actions, query, parent, null);
    }

    public class ContentBuilderStep4
    {
        private readonly string id;
        private readonly string type;
        private readonly ContentActions actions;
        private readonly ContentQuery query;
        private readonly ContentQuery? parent;
        private readonly IEnumerable<IProperty>? stageOne;

        public ContentBuilderStep4(string id, string type, ContentActions actions, ContentQuery query, ContentQuery? parent, IEnumerable<IProperty>? stageOne)
        {
            this.id = id;
            this.type = type;
            this.actions = actions;
            this.query = query;
            this.parent = parent;
            this.stageOne = stageOne;
        }

        public Content StageTwoProperties(Action<PropertyBuilder> action)
        {
            var builder = new PropertyBuilder();
            action(builder);
            return new Content(id, type, actions, query, parent, stageOne, builder.Done());
        }

        public Content SkipStageTwo()
            => new Content(id, type, actions, query, parent, stageOne, null);
    }
}