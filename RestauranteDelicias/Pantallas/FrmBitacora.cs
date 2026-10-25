using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Pantallas
{
    // quien hizo que, cuando y desde que compu
    public class FrmBitacora : Form
    {
        readonly DateTimePicker desde = new DateTimePicker();
        readonly DateTimePicker hasta = new DateTimePicker();
        readonly TextBox txtBuscar = new TextBox();
        readonly DataGridView grid = new DataGridView();

        public FrmBitacora()
        {
            Estilo.Formulario(this, "Bitácora", 980, 560);

            var arriba = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
            desde.Format = hasta.Format = DateTimePickerFormat.Short;
            desde.Width = hasta.Width = 120;
            desde.Value = DateTime.Today.AddDays(-7);
            txtBuscar.Width = 220;
            txtBuscar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Cargar(); };
            var btn = Estilo.Boton("Buscar", Estilo.Gris);
            btn.Click += (s, e) => Cargar();
            arriba.Controls.Add(new Label { Text = "Desde", AutoSize = true, Margin = new Padding(4, 10, 4, 0) });
            arriba.Controls.Add(desde);
            arriba.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(8, 10, 4, 0) });
            arriba.Controls.Add(hasta);
            arriba.Controls.Add(new Label { Text = "Texto", AutoSize = true, Margin = new Padding(8, 10, 4, 0) });
            arriba.Controls.Add(txtBuscar);
            arriba.Controls.Add(btn);

            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.Columns.Add("fecha", "Fecha");
            grid.Columns.Add("usuario", "Usuario");
            grid.Columns.Add("equipo", "Compu");
            grid.Columns.Add("accion", "Acción");
            grid.Columns.Add("detalle", "Detalle");
            grid.Columns["fecha"].FillWeight = 60;
            grid.Columns["equipo"].FillWeight = 50;
            grid.Columns["detalle"].FillWeight = 220;

            Controls.Add(grid);
            Controls.Add(arriba);
            Controls.Add(Estilo.Encabezado("Bitácora"));
            Load += (s, e) => Cargar();
        }

        void Cargar()
        {
            List<EntradaBitacora> lista = null;
            if (!Ui.Seguro(this, () => lista = Bitacora.Listar(desde.Value, hasta.Value, txtBuscar.Text))) return;
            grid.Rows.Clear();
            foreach (var b in lista)
                grid.Rows.Add(b.Fecha.ToString("dd/MM HH:mm"), b.Usuario, b.Equipo, b.Accion, b.Detalle);
        }
    }
}
