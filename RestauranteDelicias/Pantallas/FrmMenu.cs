using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Pantallas
{
    public class FrmMenu : Form
    {
        readonly DataGridView grid = new DataGridView();
        readonly DataGridViewComboBoxColumn colCategoria = new DataGridViewComboBoxColumn();
        readonly CheckBox chkInactivos = new CheckBox();
        BindingList<Platillo> lista = new BindingList<Platillo>();
        readonly HashSet<Platillo> cambiados = new HashSet<Platillo>();

        public FrmMenu()
        {
            Estilo.Formulario(this, "Menú", 1100, 600);

            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Nombre", HeaderText = "Platillo", FillWeight = 160 });
            colCategoria.DataPropertyName = "CategoriaId";
            colCategoria.HeaderText = "Categoría";
            colCategoria.ValueMember = "Id";
            colCategoria.DisplayMember = "Nombre";
            colCategoria.FlatStyle = FlatStyle.Flat;
            colCategoria.FillWeight = 100;
            grid.Columns.Add(colCategoria);
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Descripcion", HeaderText = "Descripción", FillWeight = 180 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Precio",
                HeaderText = "Precio (Q)",
                FillWeight = 60,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "0.00", Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Disponible", HeaderText = "Hay hoy", FillWeight = 45 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Activo", HeaderText = "En el menú", FillWeight = 50 });

            grid.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < lista.Count) cambiados.Add(lista[e.RowIndex]);
            };
            // los checkbox no avisan el cambio hasta que se sale de la celda, asi se guarda al tiro
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewCheckBoxCell)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.DataError += (s, e) =>
            {
                MessageBox.Show(this, "Revisa el valor, el precio tiene que ser un número.", "Dato inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.ThrowException = false;
            };

            var barra = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(8), FlowDirection = FlowDirection.RightToLeft };
            var btnGuardar = Estilo.Boton("Guardar cambios", Estilo.Vino);
            var btnCerrar = Estilo.Boton("Cerrar", Estilo.Gris);
            var btnNuevo = Estilo.Boton("Nuevo platillo", Estilo.Libre);
            var btnCategoria = Estilo.Boton("Nueva categoría", Estilo.Azul);
            var btnOpciones = Estilo.Boton("Opciones del platillo", Estilo.Azul);
            var btnGrupos = Estilo.Boton("Grupos de opciones", Estilo.Gris);
            btnOpciones.Click += (s, e) => OpcionesDelPlatillo();
            btnGrupos.Click += (s, e) => { using (var f = new FrmGrupos()) f.ShowDialog(this); };
            btnGuardar.Click += (s, e) => Guardar();
            btnCerrar.Click += (s, e) => Close();
            btnNuevo.Click += (s, e) => Nuevo();
            btnCategoria.Click += (s, e) => NuevaCategoria();
            chkInactivos.Text = "Ver los que ya no están en el menú";
            chkInactivos.AutoSize = true;
            chkInactivos.Margin = new Padding(16, 12, 0, 0);
            chkInactivos.CheckedChanged += (s, e) => { if (PreguntarSiHayCambios()) Cargar(); };
            barra.Controls.AddRange(new Control[] { btnGuardar, btnCerrar, btnNuevo, btnCategoria, btnOpciones, btnGrupos, chkInactivos });

            var nota = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(8, 8, 0, 0),
                ForeColor = Estilo.Gris,
                Text = "\"Hay hoy\" es para cuando se acaba algo en el día. Para sacarlo del menú desmarca \"En el menú\", así no se pierde en las ventas anteriores."
            };

            Controls.Add(grid);
            Controls.Add(nota);
            Controls.Add(barra);
            Controls.Add(Estilo.Encabezado("Menú"));

            Load += (s, e) => Cargar();
            FormClosing += (s, e) => { if (!PreguntarSiHayCambios()) e.Cancel = true; };
        }

        void Cargar()
        {
            List<Categoria> cats = null;
            List<Platillo> platillos = null;
            if (!Ui.Seguro(this, () =>
            {
                cats = MenuRepo.Categorias(false);
                platillos = MenuRepo.Platillos(!chkInactivos.Checked);
            })) return;

            colCategoria.DataSource = cats;
            lista = new BindingList<Platillo>(platillos);
            grid.DataSource = lista;
            cambiados.Clear();
        }

        // true si se puede seguir, ya sea porque no habia cambios o porque decidio que no le importan
        bool PreguntarSiHayCambios()
        {
            grid.EndEdit();
            if (cambiados.Count == 0) return true;
            var r = MessageBox.Show(this, "Hay " + cambiados.Count + " platillo(s) con cambios sin guardar.\n¿Guardarlos?",
                "Menú", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) return false;
            if (r == DialogResult.Yes) return Guardar();
            cambiados.Clear();
            return true;
        }

        void Nuevo()
        {
            var cats = colCategoria.DataSource as List<Categoria>;
            if (cats == null || cats.Count == 0)
            {
                MessageBox.Show(this, "Primero crea una categoría.", "Menú", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var p = new Platillo { Nombre = "Nuevo platillo", CategoriaId = cats[0].Id, Precio = 0m };
            lista.Add(p);
            cambiados.Add(p);
            grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[0];
            grid.BeginEdit(true);
        }

        // lo que se le pregunta al platillo al pedirlo, como el estilo de los huevos
        void OpcionesDelPlatillo()
        {
            var p = grid.CurrentRow == null || grid.CurrentRow.Index >= lista.Count ? null : lista[grid.CurrentRow.Index];
            if (p == null) return;
            if (p.Id == 0 || cambiados.Contains(p))
            {
                MessageBox.Show(this, "Primero guarda los cambios del platillo.", "Menú", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var f = new FrmGruposDePlatillo(p))
                if (f.ShowDialog(this) == DialogResult.OK) Cargar();
        }

        void NuevaCategoria()
        {
            var nombre = FrmPregunta.Texto(this, "Nueva categoría", "¿Cómo se llama la categoría? Por ejemplo Desayunos o Licuados.", "");
            if (string.IsNullOrWhiteSpace(nombre)) return;
            var barra = Ui.Confirmar(this, "¿Lo de esta categoría se prepara en la barra de bebidas?\n(Si no, va a cocina)", "Nueva categoría");
            List<Categoria> cats = null;
            if (Ui.Seguro(this, () =>
            {
                MenuRepo.CrearCategoria(nombre.Trim(), barra ? "Barra" : "Cocina");
                cats = MenuRepo.Categorias(false);
            }))
                colCategoria.DataSource = cats;
        }

        bool Guardar()
        {
            grid.EndEdit();
            var malo = cambiados.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Nombre) || x.Precio <= 0);
            if (malo != null)
            {
                MessageBox.Show(this, "Todos los platillos necesitan nombre y un precio mayor a cero.", "Menú",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var ok = Ui.Seguro(this, () =>
            {
                foreach (var p in cambiados.ToList())
                {
                    p.Nombre = p.Nombre.Trim();
                    MenuRepo.GuardarPlatillo(p);
                    cambiados.Remove(p);
                }
            });
            if (ok)
            {
                grid.Refresh();
                MessageBox.Show(this, "Listo, el menú quedó guardado. Las otras cajas lo ven al abrir la siguiente mesa.", "Menú",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return ok;
        }
    }
}
