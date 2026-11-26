using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using RailworksForge.ViewModels;

namespace RailworksForge.Services;

public interface IDialogService
{
    TDialog Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDialog>(params object[] arguments) where TDialog : DialogViewModel;

    Task<TResult?> Show<TResult>(DialogViewModel<TResult> dialog);

    Task Show(DialogViewModel dialog);
}
