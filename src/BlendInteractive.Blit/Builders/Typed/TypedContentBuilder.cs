namespace BlendInteractive.Blit.Builders.Typed;

public class TypedContentBuilder<T>
{
    private readonly string id;
    private readonly ContentActions actions;

    public TypedContentBuilder(string id, ContentActions actions)
    {
        this.id = id;
        this.actions = actions;
    }

    public TypedContentBuilderStep2 Query(Action<TypedContentQueryBuilder<T>> query)
    {
        var builder = new TypedContentQueryBuilder<T>();
        query(builder);
        return new TypedContentBuilderStep2(id, actions, new ContentQuery(builder.Done()));
    }

    public class TypedContentBuilderStep2
    {
        private readonly string id;
        private readonly ContentActions actions;
        private readonly ContentQuery query;

        public TypedContentBuilderStep2(string id, ContentActions actions, ContentQuery query)
        {
            this.id = id;
            this.actions = actions;
            this.query = query;
        }

        public TypedContentBuilderStep3 SkipParentQuery()
            => new TypedContentBuilderStep3(id, actions, query, null);

        public TypedContentBuilderStep3 ParentQuery(Action<TypedContentQueryBuilder<T>> parentQuery)
        {
            var builder = new TypedContentQueryBuilder<T>();
            parentQuery(builder);
            return new TypedContentBuilderStep3(id, actions, query, new ContentQuery(builder.Done()));
        }
    }

    public class TypedContentBuilderStep3
    {
        private readonly string id;
        private readonly ContentActions actions;
        private readonly ContentQuery query;
        private readonly ContentQuery? parentQuery;

        public TypedContentBuilderStep3(string id, ContentActions actions, ContentQuery query, ContentQuery? parentQuery)
        {
            this.id = id;
            this.actions = actions;
            this.query = query;
            this.parentQuery = parentQuery;
        }

        public TypedContentBuilderStep4 StageOneProperties(Action<TypedPropertyBuilder<T>> action)
        {
            var builder = new TypedPropertyBuilder<T>();
            action(builder);
            return new TypedContentBuilderStep4(id, actions, query, parentQuery, builder.Done());
        }

        public TypedContentBuilderStep4 SkipStageOne()
            => new TypedContentBuilderStep4(id, actions, query, parentQuery, null);
    }

    public class TypedContentBuilderStep4
    {
        private readonly string id;
        private readonly ContentActions actions;
        private readonly ContentQuery query;
        private readonly ContentQuery? parentQuery;
        private readonly IEnumerable<IProperty>? stageOne;

        public TypedContentBuilderStep4(string id, ContentActions actions, ContentQuery query, ContentQuery? parentQuery, IEnumerable<IProperty>? stageOne)
        {
            this.id = id;
            this.actions = actions;
            this.query = query;
            this.parentQuery = parentQuery;
            this.stageOne = stageOne;
        }

        public Content StageTwoProperties(Action<TypedPropertyBuilder<T>> action)
        {
            var builder = new TypedPropertyBuilder<T>();
            action(builder);
            return new Content(id, typeof(T).GetShortTypeName(), actions, query, parentQuery, stageOne, builder.Done());
        }

        public Content SkipStageTwo()
            => new Content(id, typeof(T).GetShortTypeName(), actions, query, parentQuery, stageOne, null);
    }
}
