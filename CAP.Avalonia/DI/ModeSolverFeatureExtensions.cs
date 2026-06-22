using CAP.Avalonia.Services;
using CAP.Avalonia.Services.Solvers;
using CAP.Avalonia.ViewModels.Solvers;
using CAP_Core.Solvers.ModeSolver;
using Microsoft.Extensions.DependencyInjection;

namespace CAP.Avalonia.DI;

/// <summary>
/// Registers the mode-solver feature: a Python-subprocess service computing n_eff/n_g/
/// mode field, plus its ViewModel.
/// </summary>
internal static class ModeSolverFeatureExtensions
{
    /// <summary>Adds <see cref="IModeSolverService"/> and <see cref="ModeSolverViewModel"/>.</summary>
    public static IServiceCollection AddModeSolverFeature(this IServiceCollection services)
    {
        services.AddSingleton<IModeSolverService>(sp =>
        {
            var prefs = sp.GetRequiredService<UserPreferencesService>();
            var python = prefs.GetCustomPythonPath() ?? PythonResolution.ResolvePythonExecutable();
            var script = PythonResolution.FindScript("mode_solve.py");
            return new PythonModeSolverService(python, script);
        });
        services.AddTransient(sp => new ModeSolverViewModel(
            sp.GetRequiredService<IModeSolverService>()));
        return services;
    }
}
