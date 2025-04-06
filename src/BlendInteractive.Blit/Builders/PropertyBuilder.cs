namespace BlendInteractive.Blit.Builders;

public class PropertyBuilder : AbstractPropertyBuilder<PropertyBuilder>
{
    protected override PropertyBuilder Self => this;

    protected override PropertyBuilder CreateNew() => new PropertyBuilder();
}
