using PedidosRazorDemo.Models;

namespace PedidosRazorDemo.Services;

public class AlmacenPedidos
{
    private readonly object _lock = new();
    private int _siguienteId = 4;

    public List<Repartidor> Repartidores { get; } =
    [
        new() { Id = 1, Nombre = "Carlos Perez", Disponible = true },
        new() { Id = 2, Nombre = "Ana Marte", Disponible = true },
        new() { Id = 3, Nombre = "Luis Gomez", Disponible = false }
    ];

    public List<Pedido> Pedidos { get; }

    public AlmacenPedidos()
    {
        Pedidos =
        [
            CrearInstancia(1, "Maria Santos", "Calle Duarte #45", 850m, DateTime.Now.AddMinutes(-30), EstadoPedido.Confirmado),
            CrearInstancia(2, "Jose Jimenez", "Av. Independencia #120", 2400m, DateTime.Now.AddHours(1), EstadoPedido.Asignado, 3),
            CrearInstancia(3, "Carla Reyes", "Sector Villa Juana #12", 1200m, DateTime.Now.AddHours(2), EstadoPedido.Creado)
        ];
    }

    public (bool ok, string msg) CrearPedido(string cliente, string direccion, decimal subtotal, int minutosLimite)
    {
        if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(direccion))
            return (false, "Cliente y direccion son obligatorios.");

        if (subtotal <= 0 || minutosLimite <= 0)
            return (false, "El monto y los minutos limite deben ser mayores a 0.");

        lock (_lock)
        {
            var pedido = CrearInstancia(_siguienteId++, cliente.Trim(), direccion.Trim(), subtotal, DateTime.Now.AddMinutes(minutosLimite), EstadoPedido.Creado);
            Pedidos.Add(pedido);
            return (true, $"Pedido #{pedido.Id} creado. Envio: {pedido.CostoEnvio:N2} | Total: {pedido.Total:N2}.");
        }
    }

    public (bool ok, string msg) CambiarEstado(int pedidoId, EstadoPedido nuevoEstado)
    {
        lock (_lock)
        {
            var (pedido, error) = ObtenerPedido(pedidoId);
            if (pedido is null) return (false, error!);

            if (nuevoEstado == EstadoPedido.Asignado)
                return (false, "Usa 'Asignar' para pasar el pedido a Asignado.");

            if (!ReglasPedido.EsTransicionValida(pedido.Estado, nuevoEstado))
                return (false, $"No se puede pasar de {pedido.Estado} a {nuevoEstado}.");

            pedido.Estado = nuevoEstado;
            return (true, $"Pedido #{pedido.Id} ahora esta en {pedido.Estado}.");
        }
    }

    public (bool ok, string msg) AsignarRepartidor(int pedidoId, int repartidorId)
    {
        lock (_lock)
        {
            var (pedido, error) = ObtenerPedido(pedidoId);
            if (pedido is null) return (false, error!);

            if (pedido.Estado != EstadoPedido.Confirmado)
                return (false, $"Solo se asigna repartidor a pedidos Confirmados (actual: {pedido.Estado}).");

            var rep = Repartidores.FirstOrDefault(r => r.Id == repartidorId && r.Disponible);
            if (rep is null) return (false, "Repartidor no disponible.");

            rep.Disponible = false;
            pedido.RepartidorId = rep.Id;
            pedido.NombreRepartidor = rep.Nombre;
            pedido.Estado = EstadoPedido.Asignado;

            return (true, $"Pedido #{pedido.Id} asignado a {rep.Nombre}.");
        }
    }

    public (bool ok, string msg) RegistrarEntrega(int pedidoId)
    {
        lock (_lock)
        {
            var (pedido, error) = ObtenerPedido(pedidoId);
            if (pedido is null) return (false, error!);

            if (pedido.Estado != EstadoPedido.EnRuta)
                return (false, $"Solo se entrega un pedido EnRuta (actual: {pedido.Estado}).");

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntregaReal = DateTime.Now;

            var rep = Repartidores.FirstOrDefault(r => r.Id == pedido.RepartidorId);
            if (rep is not null) rep.Disponible = true;

            return (true, $"Pedido #{pedido.Id} entregado.");
        }
    }

    private (Pedido? pedido, string? error) ObtenerPedido(int id)
    {
        var pedido = Pedidos.FirstOrDefault(p => p.Id == id);
        return pedido is null ? (null, "Pedido no encontrado.") : (pedido, null);
    }

    private static Pedido CrearInstancia(int id, string cliente, string direccion, decimal subtotal, DateTime limite, EstadoPedido estado, int? repId = null)
    {
        var (envio, total) = ReglasPedido.CalcularCostos(subtotal);
        return new Pedido
        {
            Id = id,
            Cliente = cliente,
            Direccion = direccion,
            Subtotal = subtotal,
            CostoEnvio = envio,
            Total = total,
            Estado = estado,
            RepartidorId = repId,
            NombreRepartidor = repId == 3 ? "Luis Gomez" : null,
            FechaLimiteEntrega = limite
        };
    }
}