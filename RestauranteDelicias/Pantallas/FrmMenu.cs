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
        readonly BindingList<Platillo> lista;
        readonly DataGridView grid = new DataGridView();

        public FrmMenu(List<Platillo> menu)
        {
            Estilo.Formulario(this, "Menú", 720, 520);

            // se trabaja sobre una copia, si cancelan no se toca el menu de verdad
            lista = new BindingList<Platillo>(menu.Select(p => new Platillo
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Categoria = p.Categoria,
                Precio = p.Precio,
                Disponible = p.Disponible
            }).ToList());

            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = false;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Nombre", HeaderText = "Platillo", FillWeight = 200 });
            var colCategoria = new DataGridViewComboBoxColumn
            {
                DataPropertyName = "Categoria",
                HeaderText = "Categoría",
                FlatStyle = FlatStyle.Flat,
                FillWeight = 110
            };
            colCategoria.Items.AddRange(Almacen.Categorias);
            grid.Columns.Add(colCategoria);
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Precio",
                HeaderText = "Precio (Q)",
                FillWeight = 70,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "0.00", Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Disponible", HeaderText = "Disponible", FillWeight = 60 });
            grid.DataSource = lista;

            grid.DataError += (s, e) =>
            {
                MessageBox.Show(this, "Revisa el valor, el precio tiene que ser un número.", "Dato inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.ThrowException = false;
            };

            var barra = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(8), FlowDirection = FlowDirection.RightToLeft };
            var btnGuardar = Estilo.Boton("Guardar", Estilo.Vino);
            var btnCancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            var btnNuevo = Estilo.Boton("Nuevo platillo", Estilo.Libre);
            btnGuardar.Click += (s, e) => Guardar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            btnNuevo.Click += (s, e) => Nuevo();
            barra.Controls.AddRange(new Control[] { btnGuardar, btnCancelar, btnNuevo });

            var nota = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(8, 8, 0, 0),
                ForeColor = Estilo.Gris,
                Text = "Para quitar un platillo desmarca \"Disponible\". Así no se pierde en las ventas anteriores."
            };

            Controls.Add(grid);
            Controls.Add(nota);
            Controls.Add(barra);
            Controls.Add(Estilo.Encabezado("Menú"));
        }

        void Nuevo()
        {
            int id = lista.Count == 0 ? 1 : lista.Max(p => p.Id) + 1;
            lista.Add(new Platillo { Id = id, Nombre = "Nuevo platillo", Categoria = Almacen.Categorias[0], Precio = 0m });
            grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[0];
            grid.BeginEdit(true);
        }

        void Guardar()
        {
            grid.EndEdit();

            var malo = lista.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Nombre) || x.Precio <= 0);
            if (malo != null)
            {
                MessageBox.Show(this, "Todos los platillos necesitan nombre y un precio mayor a cero.", "Menú",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var p in lista) p.Nombre = p.Nombre.Trim();
            Almacen.GuardarMenu(lista);
            DialogResult = DialogResult.OK;
        }
    }
}
