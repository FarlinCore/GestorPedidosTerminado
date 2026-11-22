namespace PedidosRazorDemo.Models;

public enum EstadoPedido
{
    Creado,
    Confirmado,
    Asignado,
    EnRuta,
    Entregado,
    Cancelado
}

public class Repartidor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Disponible { get; set; } = true;
}

public class Pedido
{
    public int Id { get; set; }
    public string Cliente { get; set; } = "";
    public string Direccion { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal CostoEnvio { get; set; }
    public decimal Total { get; set; }
    public EstadoPedido Estado { get; set; } = EstadoPedido.Creado;
    public int? RepartidorId { get; set; }
    public string? NombreRepartidor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaLimiteEntrega { get; set; }
    public DateTime? FechaEntregaReal { get; set; }

    public bool EsPendiente => Estado is not (EstadoPedido.Entregado or EstadoPedido.Cancelado);
    public bool EstaAtrasado => EsPendiente && DateTime.Now > FechaLimiteEntrega;
}

public static class ReglasPedido
{
    public static (decimal envio, decimal total) CalcularCostos(decimal subtotal)
    {
        decimal envio = subtotal >= 2000m ? 0m : 150m;
        return (envio, subtotal + envio);
    }

    public static bool EsTransicionValida(EstadoPedido actual, EstadoPedido siguiente)
    {
        if (actual == siguiente) return false;

        return (actual, siguiente) switch
        {
            (EstadoPedido.Creado, EstadoPedido.Confirmado) => true,
            (EstadoPedido.Creado, EstadoPedido.Cancelado) => true,
            (EstadoPedido.Confirmado, EstadoPedido.Asignado) => true,
            (EstadoPedido.Confirmado, EstadoPedido.Cancelado) => true,
            (EstadoPedido.Asignado, EstadoPedido.EnRuta) => true,
            (EstadoPedido.Asignado, EstadoPedido.Cancelado) => true,
            (EstadoPedido.EnRuta, EstadoPedido.Entregado) => true,
            _ => false
        };
    }
}