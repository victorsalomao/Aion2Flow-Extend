using System.Collections.ObjectModel;
using Cloris.Aion2Flow.Domain.Progress;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cloris.Aion2Flow.ViewModels.Progress;

/// <summary>Cadastro e selecao de alts (adicionar/renomear/remover).</summary>
public sealed partial class AltsViewModel(IAltRepository repository) : ObservableObject
{
    public ObservableCollection<Alt> Alts { get; } = [];

    [ObservableProperty]
    public partial Alt? SelectedAlt { get; set; }

    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

    public event Action<Alt?>? SelectionChanged;

    partial void OnSelectedAltChanged(Alt? value)
    {
        EditName = value?.Name ?? string.Empty;
        SelectionChanged?.Invoke(value);
    }

    public async Task LoadAsync()
    {
        Alts.Clear();
        foreach (var alt in await repository.ListAsync())
        {
            Alts.Add(alt);
        }

        SelectedAlt = Alts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var name = EditName.Trim();
        if (name.Length == 0)
        {
            return;
        }

        var alt = new Alt(AltId.New(), name, string.Empty, string.Empty, 0);
        await repository.SaveAsync(alt);
        Alts.Add(alt);
        SelectedAlt = alt;
    }

    [RelayCommand]
    private async Task RenameAsync()
    {
        var name = EditName.Trim();
        if (SelectedAlt is not { } current || name.Length == 0)
        {
            return;
        }

        var renamed = current.Rename(name);
        await repository.SaveAsync(renamed);
        var index = Alts.IndexOf(current);
        Alts[index] = renamed;
        SelectedAlt = renamed;
    }

    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (SelectedAlt is not { } current)
        {
            return;
        }

        await repository.DeleteAsync(current.Id);
        Alts.Remove(current);
        SelectedAlt = Alts.FirstOrDefault();
    }
}
