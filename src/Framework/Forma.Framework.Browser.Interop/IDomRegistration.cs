namespace Forma.Framework.Browser.Interop;

/// <summary>A registration an owner's session holds and releases.</summary>
internal interface IDomRegistration
{
    /// <summary>The token the browser holds the registration under.</summary>
    string Token { get; }

    /// <summary>The entry the registration is made through.</summary>
    DomRoot Root { get; }

    /// <summary>The generation the registration is under.</summary>
    int Generation { get; }

    /// <summary>Whether the browser holds the registration as last requested.</summary>
    bool IsActive { get; }

    /// <summary>Marks the registration released.</summary>
    void Release();
}
