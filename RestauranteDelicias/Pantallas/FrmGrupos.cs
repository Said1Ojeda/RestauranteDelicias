using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Pantallas
{
    // los grupos de opciones que se le preguntan a los platillos, a la izquierda la lista y a la derecha el que se edita
    public class FrmGrupos : Form
    {
        readonly DataGridView gridGrupos = new DataGridView();
        readonly DataGridView gridOpciones = new DataGridView();
        readonly TextBox txtNombre = new TextBox();
        readonly NumericUpDown numMinimo = new NumericUpDown();
        readonly NumericUpDown numMaximo = new NumericUpDown();
        readonly ComboBox cboDepende = new ComboBox();
        readonly CheckBox chkActivo = new CheckBox();
        List<GrupoOpcion> grupos = new List<GrupoOpcion>();
        GrupoOpcion actual;

        // una opcion de otro grupo de la que puede depender este, se ve como Huevos · Estrellados
        class Dependencia
        {
            public int? OpcionId { get; set; }
            public string Texto { get; set; }

            public override string ToString()
            {
                return Texto;
            }
        }

        public FrmGrupos()
        {
            Estilo.Formulario(this, "Grupos de opciones", 1000, 640);

            var division = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12), BackColor = Estilo.Crema };
            division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
            division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
            division.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Estilo.Tabla(gridGrupos);
            gridGrupos.ReadOnly = true;
            gridGrupos.Dock = DockStyle.Fill;
            gridGrupos.Columns.Add("nombre", "Grupo");
            gridGrupos.Columns.Add("regla", "Se escoge");
            gridGrupos.SelectionChanged += (s, e) => Mostrar();

            var izquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
            var nuevo = Estilo.Boton("Nuevo grupo", Estilo.Libre, Icono.Agregar);
            nuevo.Dock = DockStyle.Bottom;
            nuevo.AutoSize = false;
            nuevo.Height = 42;
            nuevo.Click += (s, e) => Nuevo();
            izquierda.Controls.Add(gridGrupos);
            izquierda.Controls.Add(nuevo);

            division.Controls.Add(izquierda, 0, 0);
            division.Controls.Add(Editor(), 1, 0);

            var abajo = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var guardar = Estilo.Boton("Guardar grupo", Estilo.Vino);
            var cerrar = Estilo.Boton("Cerrar", Estilo.Gris);
            guardar.Click += (s, e) => Guardar();
            cerrar.Click += (s, e) => Close();
            abajo.Controls.AddRange(new Control[] { guardar, cerrar });

            Controls.Add(division);
            Controls.Add(abajo);
            Controls.Add(Estilo.Encabezado("Grupos de opciones"));
            CancelButton = cerrar;
            Load += (s, e) => Cargar(null);
        }

        Control Editor()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 6, 6, 6), BackColor = Color.White };
            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            txtNombre.MaxLength = 60;
            txtNombre.Font = Estilo.Normal;
            var campo = Estilo.Campo(txtNombre);
            campo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            numMinimo.Maximum = 20;
            numMaximo.Minimum = 1;
            numMaximo.Maximum = 20;
            cboDepende.DropDownStyle = ComboBoxStyle.DropDownList;
            cboDepende.Dock = DockStyle.Fill;
            chkActivo.Text = "Activo";
            chkActivo.AutoSize = true;

            t.Controls.Add(Etiqueta("Nombre"), 0, 0);
            t.Controls.Add(campo, 1, 0);
            t.SetColumnSpan(campo, 3);
            t.Controls.Add(Etiqueta("Mínimo"), 0, 1);
            t.Controls.Add(numMinimo, 1, 1);
            t.Controls.Add(Etiqueta("Máximo"), 2, 1);
            t.Controls.Add(numMaximo, 3, 1);
            t.Controls.Add(Etiqueta("Solo si escogen"), 0, 2);
            t.Controls.Add(cboDepende, 1, 2);
            t.SetColumnSpan(cboDepende, 3);
            t.Controls.Add(chkActivo, 1, 3);
            var ayuda = new Label
            {
                Text = "Mínimo 1 es obligatorio. Máximo 1 es escoger solo uno. Con \"Solo si escogen\" el grupo aparece únicamente cuando escogieron esa opción, por ejemplo el término de los huevos estrellados.",
                AutoSize = true,
                MaximumSize = new Size(580, 0),
                ForeColor = Estilo.Suave,
                Font = Estilo.Chica,
                Margin = new Padding(0, 6, 0, 8)
            };
            t.Controls.Add(ayuda, 0, 4);
            t.SetColumnSpan(ayuda, 4);

            Estilo.Tabla(gridOpciones);
            gridOpciones.Dock = DockStyle.Fill;
            gridOpciones.AllowUserToAddRows = true;
            gridOpciones.EditMode = DataGridViewEditMode.EditOnEnter;
            gridOpciones.Columns.Add(new DataGridViewTextBoxColumn { Name = "nombre", HeaderText = "Opción", FillWeight = 160 });
            gridOpciones.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "precio",
                HeaderText = "Precio extra",
                FillWeight = 60,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "0.00", Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            gridOpciones.Columns.Add(new DataGridViewCheckBoxColumn { Name = "activa", HeaderText = "Activa", FillWeight = 40 });
            gridOpciones.DefaultValuesNeeded += (s, e) =>
            {
                e.Row.Cells["precio"].Value = 0m;
                e.Row.Cells["activa"].Value = true;
            };

            panel.Controls.Add(gridOpciones);
            panel.Controls.Add(new Label { Text = "Opciones, escribe en la última fila para agregar otra", Dock = DockStyle.Top, Height = 28, ForeColor = Estilo.Suave });
            panel.Controls.Add(t);
            return panel;
        }

        static Label Etiqueta(string texto)
        {
            return new Label { Text = texto, AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 6, 8) };
        }

        void Cargar(int? escoger)
        {
            if (!Ui.Seguro(this, () => grupos = OpcionRepo.Todos())) return;
            gridGrupos.Rows.Clear();
            foreach (var g in grupos)
            {
                var regla = (g.Obligatorio ? "Obligatorio, " : "Opcional, ") + (g.EscogeUno ? "uno" : "hasta " + g.Maximo);
                var i = gridGrupos.Rows.Add(g.Nombre, regla);
                gridGrupos.Rows[i].Tag = g;
                if (!g.Activo) gridGrupos.Rows[i].DefaultCellStyle.ForeColor = Color.Silver;
                if (g.Id == escoger) gridGrupos.CurrentCell = gridGrupos.Rows[i].Cells[0];
            }
            Mostrar();
        }

        void Nuevo()
        {
            gridGrupos.ClearSelection();
            Editar(new GrupoOpcion { Nombre = "", Minimo = 0, Maximo = 1 });
            txtNombre.Focus();
        }

        void Mostrar()
        {
            var g = gridGrupos.CurrentRow == null ? null : gridGrupos.CurrentRow.Tag as GrupoOpcion;
            if (g != null) Editar(g);
        }

        void Editar(GrupoOpcion g)
        {
            actual = g;
            txtNombre.Text = g.Nombre;
            numMinimo.Value = g.Minimo;
            numMaximo.Value = Math.Max(1, g.Maximo);
            chkActivo.Checked = g.Activo;

            // puede depender de cualquier opcion de los otros grupos
            var dependencias = new List<Dependencia> { new Dependencia { Texto = "Siempre se pregunta" } };
            dependencias.AddRange(grupos.Where(x => x.Id != g.Id && x.Activo)
                .SelectMany(x => x.Opciones.Where(o => o.Activa).Select(o => new Dependencia { OpcionId = o.Id, Texto = x.Nombre + " · " + o.Nombre })));
            cboDepende.DataSource = dependencias;
            cboDepende.SelectedItem = dependencias.FirstOrDefault(d => d.OpcionId == g.SoloSiOpcionId) ?? dependencias[0];

            gridOpciones.Rows.Clear();
            foreach (var o in g.Opciones)
            {
                var i = gridOpciones.Rows.Add(o.Nombre, o.Precio, o.Activa);
                gridOpciones.Rows[i].Tag = o;
            }
        }

        void Guardar()
        {
            if (actual == null) return;
            gridOpciones.EndEdit();

            var opciones = new List<Opcion>();
            foreach (DataGridViewRow fila in gridOpciones.Rows)
            {
                if (fila.IsNewRow) continue;
                var nombre = Convert.ToString(fila.Cells["nombre"].Value ?? "").Trim();
                var existente = fila.Tag as Opcion;
                if (nombre.Length == 0 && existente == null) continue;
                decimal precio;
                if (!decimal.TryParse(Convert.ToString(fila.Cells["precio"].Value ?? "0"), out precio) || precio < 0)
                {
                    MessageBox.Show(this, "El precio extra de " + nombre + " tiene que ser un número.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                opciones.Add(new Opcion
                {
                    Id = existente != null ? existente.Id : 0,
                    Nombre = nombre,
                    Precio = precio,
                    Activa = fila.Cells["activa"].Value is bool && (bool)fila.Cells["activa"].Value
                });
            }

            var dependencia = cboDepende.SelectedItem as Dependencia;
            var cambio = new GrupoOpcion
            {
                Id = actual.Id,
                Nombre = txtNombre.Text,
                Minimo = (int)numMinimo.Value,
                Maximo = (int)numMaximo.Value,
                SoloSiOpcionId = dependencia == null ? null : dependencia.OpcionId,
                Activo = chkActivo.Checked,
                Opciones = opciones
            };
            var id = 0;
            if (Ui.Seguro(this, () => id = OpcionRepo.Guardar(cambio))) Cargar(id);
        }
    }

    // a que grupos se le pregunta a un platillo, en el orden en que se marcan
    public class FrmGruposDePlatillo : Form
    {
        readonly Platillo platillo;
        readonly CheckedListBox lista = new CheckedListBox();
        readonly List<int> orden = new List<int>();

        public FrmGruposDePlatillo(Platillo platillo)
        {
            this.platillo = platillo;
            Estilo.Formulario(this, "Opciones de " + platillo.Nombre, 460, 520);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            lista.Dock = DockStyle.Fill;
            lista.CheckOnClick = true;
            lista.BorderStyle = BorderStyle.None;
            lista.Font = Estilo.Grande;
            lista.ItemCheck += (s, e) =>
            {
                var g = (GrupoOpcion)lista.Items[e.Index];
                if (e.NewValue == CheckState.Checked) { if (!orden.Contains(g.Id)) orden.Add(g.Id); }
                else orden.Remove(g.Id);
            };

            var ayuda = new Label
            {
                Text = "Marca lo que se le pregunta a este platillo al pedirlo. Se pregunta en el orden en que los marques.",
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(18, 8, 18, 0),
                ForeColor = Estilo.Suave
            };
            var cuerpo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 6, 18, 6), BackColor = Color.White };
            cuerpo.Controls.Add(lista);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var ok = Estilo.Boton("Guardar", Estilo.Vino);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            ok.Click += (s, e) => Guardar();
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });

            Controls.Add(cuerpo);
            Controls.Add(ayuda);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado(platillo.Nombre));
            AcceptButton = ok;
            CancelButton = cancelar;
            Load += (s, e) => Cargar();
        }

        void Cargar()
        {
            List<GrupoOpcion> grupos = null;
            List<int> asignados = null;
            if (!Ui.Seguro(this, () =>
            {
                grupos = OpcionRepo.Todos().Where(g => g.Activo).ToList();
                asignados = OpcionRepo.GruposDePlatillo(platillo.Id);
            })) return;

            foreach (var g in grupos) lista.Items.Add(g, false);
            foreach (var id in asignados)
            {
                var i = grupos.FindIndex(g => g.Id == id);
                if (i >= 0) lista.SetItemChecked(i, true);
            }
        }

        void Guardar()
        {
            if (Ui.Seguro(this, () => OpcionRepo.AsignarAPlatillo(platillo.Id, orden.ToList()))) DialogResult = DialogResult.OK;
        }
    }
}
