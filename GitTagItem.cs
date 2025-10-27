using LibGit2Sharp;

namespace GitTool;

public class GitTagItem : BindableBase
{
    private readonly Tag _underlying;
    private string _name;

    public GitTagItem(Tag tag)
    {
        _underlying = tag;
        _name = tag.FriendlyName;
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public Tag Underlying
    {
        get => _underlying;
    }
}