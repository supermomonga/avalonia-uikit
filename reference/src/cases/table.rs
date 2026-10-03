//! `Table` (crates/component/src/table/table.rs): the declarative, display-only
//! table, three columns by five rows like the story's invoice table; and
//! `DataTable` (table/data_table.rs, state.rs) over a three-column delegate.
use super::size;
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    App, AppContext as _, Context, IntoElement, ParentElement as _, Styled as _, TextAlign, Window, div,
    prelude::FluentBuilder as _,
    px,
    component::{
        ActiveTheme as _, Sizable as _,
        table::{
            Column, DataTable, Table, TableBody, TableCell, TableDelegate, TableHead, TableHeader, TableRow,
            TableState,
        },
    },
};
use std::rc::Rc;

const HEADS: [&str; 3] = ["Invoice", "Method", "Amount"];
const ROWS: [[&str; 3]; 5] = [
    ["INV001", "Credit Card", "$250.00"],
    ["INV002", "PayPal", "$150.00"],
    ["INV003", "Bank Transfer", "$350.00"],
    ["INV004", "Credit Card", "$450.00"],
    ["INV005", "PayPal", "$550.00"],
];
// Used only with `fixed_widths`: they sum to less than the table, so no column shrinks.
const WIDTHS: [f32; 3] = [100., 120., 100.];

pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let bordered = param_bool(params, "bordered");
    let stripe = param_bool(params, "stripe");
    let fixed = param_bool(params, "fixed_widths");
    let width = param_f32(params, "width", 360.);
    Ok(Rc::new(move |_, _, cx| {
        let theme = cx.theme();
        let (border, radius, even) = (theme.border, theme.radius, theme.table_even);
        let head = |i: usize| {
            TableHead::new()
                .child(HEADS[i])
                .when(i == 2, |h| h.text_right())
                .when(fixed, |h| h.w(px(WIDTHS[i])))
        };
        let cell = |i: usize, text: &'static str| {
            TableCell::new()
                .child(text)
                .when(i == 2, |c| c.text_right())
                .when(fixed, |c| c.w(px(WIDTHS[i])))
        };
        // The story's stripes: the app fills every other body row with `table_even`.
        let body = TableBody::new().children(ROWS.iter().enumerate().map(|(r, row)| {
            TableRow::new()
                .when(stripe && r % 2 == 1, |t| t.bg(even))
                .children((0..3).map(|i| cell(i, row[i])))
        }));
        let table = Table::new()
            .with_size(size)
            .when(bordered, |t| t.border_1().border_color(border).rounded(radius))
            .child(TableHeader::new().child(TableRow::new().children((0..3).map(head))))
            .child(body);
        div().w(px(width)).child(table).into_any_element()
    }))
}

// DataTable: Name 120 / Email 140 / Amount 80 (right-aligned).
const COLUMNS: [(&str, &str, f32, bool); 3] =
    [("name", "Name", 120., false), ("email", "Email", 140., false), ("amount", "Amount", 80., true)];
const DATA: [[&str; 3]; 5] = [
    ["Ada", "ada@example.com", "$250.00"],
    ["Grace", "grace@example.com", "$150.00"],
    ["Linus", "linus@example.com", "$350.00"],
    ["Ken", "ken@example.com", "$450.00"],
    ["Barbara", "barbara@example.com", "$550.00"],
];

/// The delegate aligns its own header and cell content (DataTable leaves
/// Column::align to it), keeping the text at the top as the default render_th.
pub struct CaseDelegate {
    columns: Vec<Column>,
}

impl CaseDelegate {
    fn new() -> Self {
        let columns = COLUMNS
            .iter()
            .map(|&(key, name, width, right)| {
                let column = Column::new(key, name).width(px(width));
                if right { column.text_right() } else { column }
            })
            .collect();
        Self { columns }
    }

    fn right(&self, col: usize) -> bool {
        self.columns[col].align == TextAlign::Right
    }
}

impl TableDelegate for CaseDelegate {
    fn columns_count(&self, _: &App) -> usize {
        self.columns.len()
    }

    fn rows_count(&self, _: &App) -> usize {
        DATA.len()
    }

    fn column(&self, col_ix: usize, _: &App) -> Column {
        self.columns[col_ix].clone()
    }

    fn render_th(&mut self, col_ix: usize, _: &mut Window, _: &mut Context<TableState<Self>>) -> impl IntoElement {
        div()
            .size_full()
            .flex()
            .when(self.right(col_ix), |d| d.justify_end())
            .child(self.columns[col_ix].name.clone())
    }

    fn render_td(
        &mut self,
        row_ix: usize,
        col_ix: usize,
        _: &mut Window,
        _: &mut Context<TableState<Self>>,
    ) -> impl IntoElement {
        div().w_full().flex().when(self.right(col_ix), |d| d.justify_end()).child(DATA[row_ix][col_ix])
    }
}

pub fn data_table(params: &Params) -> Result<Builder> {
    let size = size(params);
    let stripe = param_bool(params, "stripe");
    let bordered = !param_bool(params, "borderless");
    let resizable = !param_bool(params, "fixed_columns");
    let width = param_f32(params, "width", 360.);
    // Below the last row, so the rows do not fill the body (no filler rows, last rule kept).
    let extra = param_f32(params, "extra", 10.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let state = cx.new(|cx| {
                TableState::new(CaseDelegate::new(), window, cx)
                    .col_selectable(false)
                    .col_movable(false)
                    .col_resizable(resizable)
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<TableState<CaseDelegate>>().ok())
            .expect("table state");
        let chrome = if bordered { px(2.) } else { px(0.) };
        let height = chrome + size.table_row_height() * (DATA.len() + 1) as f32 + px(extra);
        div()
            .w(px(width))
            .h(height)
            .child(DataTable::new(&state).with_size(size).stripe(stripe).bordered(bordered))
            .into_any_element()
    }))
}
