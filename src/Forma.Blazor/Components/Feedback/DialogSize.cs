namespace Forma.Blazor.Components.Feedback;

/// <summary>How wide a dialog is, every width capped at 95vw.</summary>
public enum DialogSize
{
    /// <summary>400px.</summary>
    Small,

    /// <summary>600px.</summary>
    Medium,

    /// <summary>800px.</summary>
    Large,

    /// <summary>1000px.</summary>
    ExtraLarge,

    /// <summary>95vw, filling the viewport.</summary>
    Full,
}
