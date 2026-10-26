using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // escoger a donde va la cuenta, a una mesa libre se mueve y a una ocupada se junta
    public class FrmMover : Form
    {
        readonly Orden orden;
        readonly FlowLayoutPanel panel = new FlowLayoutPanel();

        public int MesaDestinoId { get; private set; }
        public int MesaDestinoNumero { get; private set; }
        public bool Juntada { get; private set; }

        public FrmMover(Orden orden)
        {
            this.orden = orden;
            Estilo.Formulario(this, "Mover o juntar", 880, 600);
            StartPosition = FormStartPosition.CenterParent;

            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.Padding = new Padding(14);
            panel.BackColor = Estilo.Crema;
            panel.Resize += (s, e) => Acomodar();

            var ayuda = new Label
            {
                Text = "Toca una mesa libre para pasar ahí la cuenta, o una ocupada para juntar las dos cuentas en esa.",
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(20, 0, 20, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Estilo.Suave,
                BackColor = Color.White
            };

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var cerrar = Estilo.Boton("Cancelar", Estilo.Gris);
            cerrar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.Add(cerrar);

            Controls.Add(panel);
            Controls.Add(ayuda);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Mover o juntar " + orden.Titulo));
            CancelButton = cerrar;
            Load += (s, e) => Cargar();
        }

        void Cargar()
        {
            List<MesaEstado> mesas = null;
            if (!Ui.Seguro(this, () => mesas = MesaRepo.Estado()))
            {
                DialogResult = DialogResult.Cancel;
                return;
            }

            panel.SuspendLayout();
            foreach (var m in mesas.Where(x => x.OrdenId != orden.Id))
            {
                var mesa = m;
                var t = new Tarjeta
                {
                    Titulo = "Mesa " + mesa.Numero,
                    Detalle = mesa.AreaNombre + " · " + mesa.Capacidad + " personas",
                    Etiqueta = mesa.Ocupada ? "Juntar" : "Mover aquí",
                    Acento = mesa.Ocupada ? Estilo.Naranja : Estilo.Libre,
                    Pie = mesa.Ocupada ? Ticket.Q(mesa.Subtotal) : "",
                    Size = new Size(Estilo.Px(190), Estilo.Px(118)),
                    Margin = new Padding(Estilo.Px(6))
                };
                t.Click += (s, e) => Escoger(mesa);
                panel.Controls.Add(t);
            }
            Acomodar();
            panel.ResumeLayout();
        }

        void Acomodar()
        {
            var disponible = panel.ClientSize.Width - panel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            if (disponible > 0) Estilo.RepartirTarjetas(panel, disponible, Estilo.Px(170), Estilo.Px(230));
        }

        void Escoger(MesaEstado mesa)
        {
            if (mesa.Ocupada)
            {
                if (!Ui.Confirmar(this, "Todo lo de " + orden.Titulo + " pasa a la mesa " + mesa.Numero +
                        ", con sus platillos y lo que ya pagaron. " + orden.Titulo + " queda libre.\n\n¿Juntar?", "Juntar cuentas"))
                    return;
                if (!Ui.Seguro(this, () => OrdenRepo.Juntar(orden.Id, mesa.OrdenId.Value))) return;
                Juntada = true;
            }
            else if (!Ui.Seguro(this, () => OrdenRepo.Mover(orden.Id, mesa.Id)))
                return;

            MesaDestinoId = mesa.Id;
            MesaDestinoNumero = mesa.Numero;
            DialogResult = DialogResult.OK;
        }
    }
}
