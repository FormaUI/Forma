namespace Forma.Framework.Browser.Interop;

/// <summary>A registration an owner's session holds and releases.</summary>
internal interface IDomRegistration
{
    /// <summary>The token the browser holds the registration under.</summary>
    string Token { get; }

    /// <summary>The entry the registration is made through.</summary>
    DomRoot Root { get; }

    /// <summary>Marks the registration released, making no call; the session's own release reaches the browser.</summary>
    void Release();
}
