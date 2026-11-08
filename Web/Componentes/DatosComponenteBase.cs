using DetectorFuerzaBruta.Web.Servicios;
using Microsoft.AspNetCore.Components;

namespace DetectorFuerzaBruta.Web.Componentes;

// Base para componentes que dependen del estado compartido de DatosServicio.
// Garantiza que la vista se actualice cuando cambian los registros, la
// referencia o avanza el monitoreo simulado.
public abstract class DatosComponenteBase : ComponentBase, IDisposable
{
    [Inject]
    protected DatosServicio Datos { get; set; } = default!;

    protected override void OnInitialized()
    {
        Datos.Cambio += AlCambiar;
    }

    private void AlCambiar()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        Datos.Cambio -= AlCambiar;
    }
}
