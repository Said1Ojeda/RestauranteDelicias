using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Pantallas
{
    // el admin arma el salon, las areas a la izquierda y las mesas a la derecha
    // nada se borra, lo que ya no se usa se desactiva para no perder el historial de ventas
    public class FrmMesas : Form
    {
        readonly DataGridView gridAreas = new DataGridView();
        readonly DataGridView gridMesas = new DataGridView();
        List<Area> areas = new List<Area>();
        List<Mesa> mesas = new List<Mesa>();

        public FrmMesas()
        {
            Estilo.Formulario(this, "Mesas y áreas", 980, 600);

            var division = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12), BackColor = Estilo.Crema };
            division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            division.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Estilo.Tabla(gridAreas);
            gridAreas.ReadOnly = true;
            gridAreas.Columns.Add("nombre", "Área");
            gridAreas.Columns.Add("estado", "Estado");
            gridAreas.CellDoubleClick += (s, e) => RenombrarArea();

            Estilo.Tabla(gridMesas);
            gridMesas.ReadOnly = true;
            gridMesas.Columns.Add("numero", "Mesa");
            gridMesas.Columns.Add("area", "Área");
            gridMesas.Columns.Add("capacidad", "Personas");
            gridMesas.Columns.Add("forma", "Forma");
            gridMesas.Columns.Add("estado", "Estado");
            gridMesas.CellDoubleClick += (s, e) => EditarMesa(Seleccionada(gridMesas, mesas));

            var nuevaArea = Estilo.Boton("Nueva", Estilo.Libre, Icono.Agregar);
            var renombrar = Estilo.Boton("Renombrar", Estilo.Gris);
            var activarArea = Estilo.Boton("Activar o desactivar", Estilo.Gris);
            nuevaArea.Click += (s, e) => NuevaArea();
            renombrar.Click += (s, e) => RenombrarArea();
            activarArea.Click += (s, e) => CambiarArea();

            var nuevaMesa = Estilo.Boton("Nueva mesa", Estilo.Libre, Icono.Agregar);
            var editar = Estilo.Boton("Editar", Estilo.Gris);
            var activarMesa = Estilo.Boton("Activar o desactivar", Estilo.Gris);
            nuevaMesa.Click += (s, e) => EditarMesa(null);
            editar.Click += (s, e) => EditarMesa(Seleccionada(gridMesas, mesas));
            activarMesa.Click += (s, e) => CambiarMesa();

            division.Controls.Add(Columna("Áreas", gridAreas, nuevaArea, renombrar, activarArea), 0, 0);
            division.Controls.Add(Columna("Mesas", gridMesas, nuevaMesa, editar, activarMesa), 1, 0);

            var abajo = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var cerrar = Estilo.Boton("Cerrar", Estilo.Gris);
            cerrar.Click += (s, e) => Close();
            abajo.Controls.Add(cerrar);

            Controls.Add(division);
            Controls.Add(abajo);
            Controls.Add(Estilo.Encabezado("Mesas y áreas"));
            CancelButton = cerrar;
            Load += (s, e) => Cargar();
        }

        static Control Columna(string titulo, DataGridView grid, params Control[] botones)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = Estilo.Crema };
            var barra = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, BackColor = Estilo.Crema, Padding = new Padding(0, 6, 0, 0) };
            barra.Controls.AddRange(botones);
            grid.Dock = DockStyle.Fill;
            panel.Controls.Add(grid);
            panel.Controls.Add(barra);
            panel.Controls.Add(new Label { Text = titulo, Dock = DockStyle.Top, Height = 30, Font = Estilo.Grande, ForeColor = Estilo.Texto });
            return panel;
        }

        static T Seleccionada<T>(DataGridView grid, List<T> lista) where T : class
        {
            return grid.CurrentRow == null || grid.CurrentRow.Index >= lista.Count ? null : lista[grid.CurrentRow.Index];
        }

        void Cargar()
        {
            if (!Ui.Seguro(this, () =>
            {
                areas = MesaRepo.Areas();
                mesas = MesaRepo.Todas();
            })) return;

            gridAreas.Rows.Clear();
            foreach (var a in areas)
            {
                var i = gridAreas.Rows.Add(a.Nombre, a.Activa ? "Activa" : "Desactivada");
                if (!a.Activa) gridAreas.Rows[i].DefaultCellStyle.ForeColor = Color.Silver;
            }
            gridMesas.Rows.Clear();
            foreach (var m in mesas)
            {
                var i = gridMesas.Rows.Add(m.Numero, m.AreaNombre, m.Capacidad, m.Forma, m.Activa ? "Activa" : "Desactivada");
                if (!m.Activa) gridMesas.Rows[i].DefaultCellStyle.ForeColor = Color.Silver;
            }
        }

        void NuevaArea()
        {
            var nombre = FrmPregunta.Texto(this, "Nueva área", "¿Cómo se llama el área? Por ejemplo Segundo nivel o Jardín.", "");
            if (string.IsNullOrWhiteSpace(nombre)) return;
            if (Ui.Seguro(this, () => MesaRepo.GuardarArea(new Area { Nombre = nombre }))) Cargar();
        }

        void RenombrarArea()
        {
            var area = Seleccionada(gridAreas, areas);
            if (area == null) return;
            var nombre = FrmPregunta.Texto(this, "Renombrar área", "Nuevo nombre para " + area.Nombre + ".", area.Nombre);
            if (string.IsNullOrWhiteSpace(nombre)) return;
            var cambio = new Area { Id = area.Id, Nombre = nombre, Activa = area.Activa };
            if (Ui.Seguro(this, () => MesaRepo.GuardarArea(cambio))) Cargar();
        }

        void CambiarArea()
        {
            var area = Seleccionada(gridAreas, areas);
            if (area == null) return;
            var cambio = new Area { Id = area.Id, Nombre = area.Nombre, Activa = !area.Activa };
            if (Ui.Seguro(this, () => MesaRepo.GuardarArea(cambio))) Cargar();
        }

        void EditarMesa(Mesa mesa)
        {
            var activas = areas.Where(a => a.Activa).ToList();
            if (activas.Count == 0)
            {
                MessageBox.Show(this, "Primero crea un área.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var siguiente = mesas.Count == 0 ? 1 : mesas.Max(m => m.Numero) + 1;
            using (var f = new FrmMesa(mesa, activas, siguiente))
                if (f.ShowDialog(this) == DialogResult.OK) Cargar();
        }

        void CambiarMesa()
        {
            var mesa = Seleccionada(gridMesas, mesas);
            if (mesa == null) return;
            var cambio = new Mesa { Id = mesa.Id, Numero = mesa.Numero, AreaId = mesa.AreaId, Capacidad = mesa.Capacidad, Forma = mesa.Forma, Activa = !mesa.Activa };
            if (Ui.Seguro(this, () => MesaRepo.GuardarMesa(cambio))) Cargar();
        }
    }

    // datos de una mesa nueva o de una que ya existe
    public class FrmMesa : Form
    {
        readonly Mesa mesa;
        readonly NumericUpDown numNumero = new NumericUpDown();
        readonly ComboBox cboArea = new ComboBox();
        readonly NumericUpDown numCapacidad = new NumericUpDown();
        readonly ComboBox cboForma = new ComboBox();

        public FrmMesa(Mesa existente, List<Area> areas, int siguiente)
        {
            mesa = existente == null ? new Mesa { Numero = siguiente, AreaId = areas[0].Id } : new Mesa
            {
                Id = existente.Id, Numero = existente.Numero, AreaId = existente.AreaId,
                Capacidad = existente.Capacidad, Forma = existente.Forma, Activa = existente.Activa
            };
            var titulo = existente == null ? "Mesa nueva" : "Mesa " + existente.Numero;
            Estilo.Formulario(this, titulo, 420, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            numNumero.Minimum = 1;
            numNumero.Maximum = 9999;
            numNumero.Value = mesa.Numero;
            cboArea.DropDownStyle = ComboBoxStyle.DropDownList;
            cboArea.DataSource = areas;
            cboArea.DisplayMember = "Nombre";
            cboArea.ValueMember = "Id";
            numCapacidad.Minimum = 1;
            numCapacidad.Maximum = 30;
            numCapacidad.Value = mesa.Capacidad;
            cboForma.DropDownStyle = ComboBoxStyle.DropDownList;
            cboForma.Items.AddRange(new object[] { "Cuadrada", "Redonda", "Barra" });
            cboForma.SelectedItem = mesa.Forma;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(22, 16, 22, 8), BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var fila = 0;
            foreach (var par in new[] { new KeyValuePair<string, Control>("Número", numNumero), new KeyValuePair<string, Control>("Área", cboArea),
                                        new KeyValuePair<string, Control>("Personas", numCapacidad), new KeyValuePair<string, Control>("Forma", cboForma) })
            {
                par.Value.Dock = DockStyle.Fill;
                par.Value.Margin = new Padding(3, 6, 3, 6);
                t.Controls.Add(new Label { Text = par.Key, AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left }, 0, fila);
                t.Controls.Add(par.Value, 1, fila);
                fila++;
            }

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var ok = Estilo.Boton("Guardar", Estilo.Vino);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            ok.Click += (s, e) => Guardar();
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado(titulo));
            AcceptButton = ok;
            CancelButton = cancelar;
            Load += (s, e) => cboArea.SelectedValue = mesa.AreaId;
        }

        void Guardar()
        {
            mesa.Numero = (int)numNumero.Value;
            if (cboArea.SelectedValue == null)
            {
                MessageBox.Show(this, "Escoge el área de la mesa.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            mesa.AreaId = (int)cboArea.SelectedValue;
            mesa.Capacidad = (int)numCapacidad.Value;
            mesa.Forma = (string)cboForma.SelectedItem ?? "Cuadrada";
            if (Ui.Seguro(this, () => MesaRepo.GuardarMesa(mesa))) DialogResult = DialogResult.OK;
        }
    }
}
