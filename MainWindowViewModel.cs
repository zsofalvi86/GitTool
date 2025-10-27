using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LibGit2Sharp;
using Microsoft.Win32;

namespace GitTool;

public class MainWindowViewModel : BindableBase
{
    private readonly Queue _queue;
    private string? _repoName;
    private ObservableCollection<GitTagItem> _tags = new();
    private string _repoInfo;
    private string _queueInfo;

    public MainWindowViewModel()
    {
        _queue = new Queue(c => QueueInfo = UpdateQueueInfo(c));
        QueueInfo = UpdateQueueInfo(0);
        OpenFolderCommand = new RelayCommand(
            _ => true,
            _ =>
            {
                var dialog = new OpenFolderDialog();
                if (dialog.ShowDialog() == true)
                {
                    if (!Repository.IsValid(dialog.FolderName))
                    {
                        MessageBox.Show("Path is not a GIT repository", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    _repoName = dialog.FolderName;
                    using var repo = new Repository(_repoName);
                    Tags = new ObservableCollection<GitTagItem>(GetTags(repo));
                    RepoInfo = UpdateRepoInfo(repo);
                }
            });

        DeleteTagCommand = new RelayCommand(
            p => _repoName != null && p is IList pList && pList.Count > 0,
            p =>
            {
                using var repo = new Repository(_repoName);
                foreach (var el in p as IList)
                {
                    if (el is GitTagItem tag)
                    {
                        
                        repo.Tags.Remove(tag.Underlying);
                        _queue.Enqueue(() =>
                        {
                            using var localRepo = new Repository(_repoName);
                            localRepo.Network.Push(localRepo.Network.Remotes["origin"],
                                $":{tag.Underlying.CanonicalName}",
                                new PushOptions()
                                {
                                    CredentialsProvider = GitCredentialsProvider.GetHandler(),
                                });
                        });
                    }
                }

                Tags = new ObservableCollection<GitTagItem>(GetTags(repo));
            }
        );
    }

    private IEnumerable<GitTagItem> GetTags(Repository repo)
    {
        return repo.Tags.Select(tag => new GitTagItem(tag));
    }

    private string UpdateRepoInfo(Repository repo)
    {
        var untrackedCount = repo.RetrieveStatus(new StatusOptions()).Untracked.Count();
        return $"Branch: {repo.Head.FriendlyName}, untracked: {untrackedCount}";
    }

    private string UpdateQueueInfo(int items)
    {
        return $"Queued GIT commands: {items}";
    }

    public ICommand OpenFolderCommand { get; }

    public ICommand DeleteTagCommand { get; }

    public ObservableCollection<GitTagItem> Tags
    {
        get => _tags;
        set => SetProperty(ref _tags, value);
    }

    public string RepoInfo
    {
        get => _repoInfo;
        set => SetProperty(ref _repoInfo, value);
    }

    public string QueueInfo
    {
        get => _queueInfo;
        set => SetProperty(ref _queueInfo, value);
    }
}