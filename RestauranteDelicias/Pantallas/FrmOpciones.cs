using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // al pedir un platillo con opciones se escoge lo de cada grupo, como el estilo de los huevos y los extras
    // un grupo que depende de otra opcion aparece solo cuando esa opcion esta escogida
    public class FrmOpciones : Form
    {
        readonly Platillo platillo;
        readonly List<GrupoOpcion> grupos;
        readonly HashSet<int> escogidas = new HashSet<int>();
        readonly Dictionary<int, Control> bloques = new Dictionary<int, Control>();
        readonly Dictionary<int, Boton> botones = new Dictionary<int, Boton>();
        readonly FlowLayoutPanel lista = new FlowLayoutPanel();
        readonly NumericUpDown numCantidad = new NumericUpDown();
        readonly TextBox txtNota = new TextBox();
        readonly Label lblFalta = new Label();
        readonly Boton btnAgregar;

        public List<int> Opciones { get; private set; }
        public int Cantidad { get; private set; }
        public string Nota { get; private set; }

        public FrmOpciones(Platillo platillo, List<GrupoOpcion> grupos, int cantidad, string nota)
        {
            this.platillo = platillo;
            this.grupos = grupos;
            Estilo.Formulario(this, platillo.Nombre, 640, 660);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            lista.Dock = DockStyle.Fill;
            lista.FlowDirection = FlowDirection.TopDown;
            lista.WrapContents = false;
            lista.AutoScroll = true;
            lista.Padding = new Padding(20, 10, 20, 10);
            lista.BackColor = Color.White;
            foreach (var g in grupos) lista.Controls.Add(Bloque(g));

            var pie = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 132, ColumnCount = 4, Padding = new Padding(20, 10, 20, 6), BackColor = Estilo.Crema };
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            numCantidad.Minimum = 1;
            numCantidad.Maximum = 50;
            numCantidad.Value = Math.Max(1, Math.Min(50, cantidad));
            numCantidad.Width = 64;
            numCantidad.Font = Estilo.Grande;
            numCantidad.ValueChanged += (s, e) => Revisar();
            txtNota.MaxLength = 150;
            txtNota.Text = nota ?? "";
            txtNota.Font = Estilo.Normal;
            var campoNota = Estilo.Campo(txtNota);
            campoNota.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            pie.Controls.Add(new Label { Text = "Cantidad", AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left }, 0, 0);
            pie.Controls.Add(numCantidad, 1, 0);
            pie.Controls.Add(new Label { Text = "Nota", AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left, Margin = new Padding(14, 3, 3, 3) }, 2, 0);
            pie.Controls.Add(campoNota, 3, 0);
            lblFalta.AutoSize = true;
            lblFalta.ForeColor = Estilo.Naranja;
            lblFalta.Font = Estilo.Negrita;
            lblFalta.Margin = new Padding(0, 8, 0, 0);
            pie.Controls.Add(lblFalta, 0, 1);
            pie.SetColumnSpan(lblFalta, 4);

            var barra = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(14, 12, 14, 12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            btnAgregar = Estilo.Boton("Agregar", Estilo.Libre, Icono.Agregar);
            btnAgregar.Font = Estilo.Grande;
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            btnAgregar.Click += (s, e) => Aceptar();
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            barra.Controls.AddRange(new Control[] { btnAgregar, cancelar });

            Controls.Add(lista);
            Controls.Add(pie);
            Controls.Add(barra);
            Controls.Add(Estilo.Encabezado(platillo.Nombre + " · " + Ticket.Q(platillo.Precio)));
            CancelButton = cancelar;
            Load += (s, e) => Revisar();
        }

        Control Bloque(GrupoOpcion g)
        {
            var bloque = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Margin = new Padding(0, 6, 0, 10) };
            var regla = g.Obligatorio ? (g.EscogeUno ? "escoge uno" : "escoge de " + g.Minimo + " a " + g.Maximo)
                                      : (g.EscogeUno ? "opcional" : "opcional, hasta " + g.Maximo);
            bloque.Controls.Add(new Label { Text = g.Nombre, AutoSize = true, Font = Estilo.Grande, Margin = new Padding(0) });
            bloque.Controls.Add(new Label { Text = regla, AutoSize = true, ForeColor = g.Obligatorio ? Estilo.Vino : Estilo.Suave, Font = Estilo.Chica, Margin = new Padding(0, 0, 0, 4) });

            var chips = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(560, 0), Margin = new Padding(0) };
            foreach (var o in g.Opciones)
            {
                var opcion = o;
                var b = Estilo.Chip(o.Precio > 0 ? o.Nombre + "  +" + Ticket.Q(o.Precio) : o.Nombre, Estilo.Vino);
                b.Radio = 10;
                b.Padding = new Padding(14, 8, 14, 8);
                b.Click += (s, e) => Tocar(g, opcion);
                botones[o.Id] = b;
                chips.Controls.Add(b);
            }
            bloque.Controls.Add(chips);
            bloques[g.Id] = bloque;
            return bloque;
        }

        // en un grupo de una sola se cambia la que estaba, en uno de varias se prende y se apaga hasta el maximo
        void Tocar(GrupoOpcion g, Opcion o)
        {
            if (escogidas.Contains(o.Id))
                escogidas.Remove(o.Id);
            else if (g.EscogeUno)
            {
                foreach (var otra in g.Opciones) escogidas.Remove(otra.Id);
                escogidas.Add(o.Id);
            }
            else if (g.Opciones.Count(x => escogidas.Contains(x.Id)) < g.Maximo)
                escogidas.Add(o.Id);
            Revisar();
        }

        bool Aplica(GrupoOpcion g)
        {
            return !g.SoloSiOpcionId.HasValue || escogidas.Contains(g.SoloSiOpcionId.Value);
        }

        void Revisar()
        {
            // lo de un grupo que ya no aplica se desmarca, asi no se cuela en la orden
            foreach (var g in grupos.Where(x => !Aplica(x)))
                foreach (var o in g.Opciones) escogidas.Remove(o.Id);

            foreach (var g in grupos)
            {
                bloques[g.Id].Visible = Aplica(g);
                foreach (var o in g.Opciones) botones[o.Id].Marcado = escogidas.Contains(o.Id);
            }

            var falta = grupos.FirstOrDefault(g => Aplica(g) && g.Opciones.Count(o => escogidas.Contains(o.Id)) < g.Minimo);
            lblFalta.Text = falta != null ? "Falta escoger " + falta.Nombre.ToLower() : "";
            var precio = platillo.Precio + grupos.SelectMany(g => g.Opciones).Where(o => escogidas.Contains(o.Id)).Sum(o => o.Precio);
            btnAgregar.Text = "Agregar " + Ticket.Q(precio * numCantidad.Value);
            btnAgregar.Enabled = falta == null;
        }

        void Aceptar()
        {
            if (!btnAgregar.Enabled) return;
            Opciones = escogidas.ToList();
            Cantidad = (int)numCantidad.Value;
            Nota = txtNota.Text.Trim();
            DialogResult = DialogResult.OK;
        }
    }
}
