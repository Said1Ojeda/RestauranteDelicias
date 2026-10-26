using System;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // abrir la caja con el fondo y cerrarla contando el efectivo
    public class FrmCaja : Form
    {
        readonly NumericUpDown numMonto = new NumericUpDown();
        readonly TextBox txtNotas = new TextBox();
        readonly Label lblDiferencia = new Label();
        Turno turno;
        ResumenTurno resumen;

        public FrmCaja()
        {
            Estilo.Formulario(this, "Caja", 480, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Load += (s, e) => Armar();
        }

        void Armar()
        {
            if (!Ui.Seguro(this, () =>
            {
                turno = TurnoRepo.Abierto(Sesion.Id.Value);
                if (turno != null) resumen = TurnoRepo.Resumen(turno.Id);
            }))
            {
                Close();
                return;
            }
            // si la cerraron desde otra compu esta sesion se entera aca
            Sesion.TurnoId = turno != null ? turno.Id : (int?)null;

            Controls.Clear();
            numMonto.DecimalPlaces = 2;
            numMonto.Maximum = 100000;
            numMonto.ThousandsSeparator = true;
            numMonto.Font = Estilo.Titulo;
            numMonto.Width = 200;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(18), AutoScroll = true };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(10), FlowDirection = FlowDirection.RightToLeft };
            var cerrar = Estilo.Boton("Cerrar ventana", Estilo.Gris);
            cerrar.Click += (s, e) => Close();

            if (turno == null)
            {
                Text = "Abrir caja";
                Dato(t, "Cajero", Sesion.Nombre);
                Dato(t, "Fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                t.Controls.Add(new Label { Text = "Fondo inicial (Q)", AutoSize = true, Font = Estilo.Negrita, Margin = new Padding(3, 18, 3, 3) });
                t.Controls.Add(numMonto);
                var ayuda = new Label
                {
                    Text = "Lo que hay en la gaveta para dar vuelto antes de la primera venta.",
                    AutoSize = true,
                    MaximumSize = new Size(400, 0),
                    ForeColor = Estilo.Gris
                };
                t.Controls.Add(ayuda);
                t.SetColumnSpan(ayuda, 2);

                var abrir = Estilo.Boton("Abrir caja", Estilo.Libre);
                abrir.Click += (s, e) => Abrir();
                botones.Controls.AddRange(new Control[] { abrir, cerrar });
                AcceptButton = abrir;
            }
            else
            {
                Text = "Cerrar caja";
                Dato(t, "Cajero", turno.Cajero);
                Dato(t, "Abierta desde", turno.Apertura.ToString("dd/MM/yyyy HH:mm"));
                Dato(t, "Ventas cobradas", resumen.Ventas.ToString());
                Dato(t, "Fondo inicial", Ticket.Q(resumen.MontoInicial));
                Dato(t, "Cobrado en efectivo", Ticket.Q(resumen.Efectivo));
                Dato(t, "Cobrado con tarjeta", Ticket.Q(resumen.Tarjeta));
                Dato(t, "Transferencias", Ticket.Q(resumen.Transferencia));
                Dato(t, "Propinas incluidas", Ticket.Q(resumen.Propinas));
                Dato(t, "Debe haber en efectivo", Ticket.Q(resumen.EsperadoEnCaja), true);

                t.Controls.Add(new Label { Text = "Efectivo contado (Q)", AutoSize = true, Font = Estilo.Negrita, Margin = new Padding(3, 14, 3, 3) });
                t.Controls.Add(numMonto);
                numMonto.ValueChanged += (s, e) => Diferencia();

                lblDiferencia.AutoSize = true;
                lblDiferencia.Font = Estilo.Negrita;
                t.Controls.Add(new Label { Text = "Diferencia", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
                t.Controls.Add(lblDiferencia);

                txtNotas.Dock = DockStyle.Fill;
                txtNotas.MaxLength = 300;
                t.Controls.Add(new Label { Text = "Notas", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
                t.Controls.Add(txtNotas);

                var cerrarCaja = Estilo.Boton("Cerrar caja e imprimir corte", Estilo.Vino);
                cerrarCaja.Click += (s, e) => CerrarCaja();
                botones.Controls.AddRange(new Control[] { cerrarCaja, cerrar });
                Diferencia();
            }

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado(Text));
        }

        static void Dato(TableLayoutPanel t, string etiqueta, string valor, bool resaltar = false)
        {
            t.Controls.Add(new Label { Text = etiqueta, AutoSize = true, Margin = new Padding(3, 5, 3, 3), Font = resaltar ? Estilo.Negrita : Estilo.Normal });
            t.Controls.Add(new Label { Text = valor, AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(3, 5, 3, 3), Font = resaltar ? Estilo.Negrita : Estilo.Normal });
        }

        void Diferencia()
        {
            var dif = numMonto.Value - resumen.EsperadoEnCaja;
            lblDiferencia.Text = dif == 0 ? "Cuadra exacto" : (dif > 0 ? "Sobran " : "Faltan ") + Ticket.Q(Math.Abs(dif));
            lblDiferencia.ForeColor = dif == 0 ? Estilo.Libre : (dif > 0 ? Estilo.Azul : Estilo.Ocupada);
        }

        void Abrir()
        {
            int id = 0;
            if (!Ui.Seguro(this, () => id = TurnoRepo.Abrir(Sesion.Id.Value, numMonto.Value))) return;
            Sesion.TurnoId = id;
            MessageBox.Show(this, "Caja abierta. Ya puedes cobrar.", "Caja", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }

        void CerrarCaja()
        {
            var dif = numMonto.Value - resumen.EsperadoEnCaja;
            var aviso = dif == 0 ? "La caja cuadra exacto." : "Hay una diferencia de " + Ticket.Q(dif) + ".";
            if (!Ui.Confirmar(this, aviso + "\n\nDespués de cerrar ya no se puede cobrar con este turno. ¿Cerrar la caja?", "Cerrar caja"))
                return;

            var contado = numMonto.Value;
            if (!Ui.Seguro(this, () => TurnoRepo.Cerrar(turno.Id, contado, txtNotas.Text.Trim()))) return;
            Sesion.TurnoId = null;

            Turno cerrado = null;
            if (Ui.Seguro(this, () => cerrado = TurnoRepo.Obtener(turno.Id)) && cerrado != null)
                Ui.Seguro(this, () => Ticket.Imprimir(Ticket.Corte(cerrado, resumen, txtNotas.Text.Trim()), this));
            DialogResult = DialogResult.OK;
        }
    }
}
