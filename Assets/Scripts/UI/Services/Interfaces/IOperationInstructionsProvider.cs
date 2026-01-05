using System;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface IOperationInstructionsProvider
    {
        string GetInstructions();
        event Action<string> OnChanged;
    }
}
