namespace Lost;

using System.Collections;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;

using static Windows.Win32.PInvoke;

#if NET6_0_OR_GREATER
[SupportedOSPlatform("windows7.0")]
#endif
public sealed class EnvironmentVariables: INotifyPropertyChanged {
    public static EnvironmentVariables Vars { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    readonly Dictionary<string, string> vars = new();
    readonly Thread thread;
    readonly TaskCompletionSource<bool> started = new();

    public string? this[string key] {
        get {
            lock (this.vars) {
                return this.vars.TryGetValue(key, out string? value) ? value : null;
            }
        }
    }

    void Refresh() {
        var newVars = Get();
        string[] removed = this.vars.Keys.Except(newVars.Keys).ToArray();

        foreach (string key in removed) {
            this.vars.Remove(key);
            this.OnPropertyChanged(key);
        }

        foreach (var var in newVars) {
            this.vars[var.Key] = var.Value;
            this.OnPropertyChanged(var.Key);
        }
    }

    unsafe void MessageLoop() {
        var hwnd = CreateWindowEx(dwExStyle: default,
                                  lpClassName: "STATIC",
                                  lpWindowName: "EnvironmentVariables",
                                  dwStyle: default,
                                  X: 0, Y: 0,
                                  nWidth: 0, nHeight: 0,
                                  hWndParent: default,
                                  hMenu: default,
                                  hInstance: default,
                                  lpParam: default);

        if (hwnd == IntPtr.Zero)
            this.started.SetException(new Win32Exception());

        foreach (var entry in Get())
            this.vars.Add(entry.Key, entry.Value);

        SystemEvents.UserPreferenceChanged += (sender, args) => {
            if (args.Category == UserPreferenceCategory.General)
                lock (this.vars) {
                    this.Refresh();
                }
        };
        this.started.SetResult(true);

        // message loop
        while (GetMessage(out var msg, hWnd: hwnd, wMsgFilterMin: 0, wMsgFilterMax: 0) != 0) {
            TranslateMessage(in msg);
            DispatchMessage(in msg);
        }
    }

    static Dictionary<string, string> Get()
        => Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)
                      .Cast<DictionaryEntry>()
                      .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    EnvironmentVariables() {
        this.thread = new(this.MessageLoop) {
            Name = nameof(EnvironmentVariables),
            IsBackground = true,
        };
        this.thread.SetApartmentState(ApartmentState.STA);
        this.thread.Start();
        this.started.Task.Wait();
    }


    void OnPropertyChanged(string propertyName)
        => this.PropertyChanged?.Invoke(this, new(propertyName));
}