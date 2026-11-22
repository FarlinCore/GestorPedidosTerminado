using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PedidosRazorDemo.Models;
using PedidosRazorDemo.Services;

namespace PedidosRazorDemo.Pages;

public class IndexModel(AlmacenPedidos almacen) : PageModel
{
    [BindProperty] public string Cliente { get; set; } = "";
    [BindProperty] public string Direccion { get; set; } = "";
    [BindProperty] public decimal Subtotal { get; set; }
    [BindProperty] public int MinutosLimite { get; set; } = 60;
    [BindProperty(SupportsGet = true)] public bool SoloPendientes { get; set; }

    public string? Mensaje { get; set; }
    public bool EsError { get; set; }
    public List<Pedido> PedidosMostrados { get; set; } = [];
    public List<Repartidor> RepartidoresDisponibles { get; set; } = [];

    public void OnGet()
    {
        Mensaje = TempData["Mensaje"] as string;
        EsError = TempData["EsError"] as bool? ?? false;

        var query = almacen.Pedidos.AsEnumerable();
        if (SoloPendientes) query = query.Where(p => p.EsPendiente);

        PedidosMostrados = query.OrderByDescending(p => p.Id).ToList();
        RepartidoresDisponibles = almacen.Repartidores.Where(r => r.Disponible).ToList();
    }

    public IActionResult OnPostCrear() => Ejecutar(() => almacen.CrearPedido(Cliente, Direccion, Subtotal, MinutosLimite));
    public IActionResult OnPostEstado(int pedidoId, EstadoPedido nuevoEstado) => Ejecutar(() => almacen.CambiarEstado(pedidoId, nuevoEstado));
    public IActionResult OnPostAsignar(int pedidoId, int repartidorId) => Ejecutar(() => almacen.AsignarRepartidor(pedidoId, repartidorId));
    public IActionResult OnPostEntregar(int pedidoId) => Ejecutar(() => almacen.RegistrarEntrega(pedidoId));

    private IActionResult Ejecutar(Func<(bool ok, string msg)> accion)
    {
        var (ok, msg) = accion();
        TempData["Mensaje"] = msg;
        TempData["EsError"] = !ok;
        return RedirectToPage(new { SoloPendientes });
    }
}