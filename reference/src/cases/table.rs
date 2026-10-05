//! `Table` (crates/component/src/table/table.rs): the declarative, display-only
//! table, three columns by five rows like the story's invoice table; and
//! `DataTable` (table/data_table.rs, state.rs) over a three-column delegate.
use super::size;
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    App, AppContext as _, Context, IntoElement, ParentElement as _, Styled as _, TextAlign, Window, div,
    prelude::FluentBuilder as _,
    px,
    component::{
        ActiveTheme as _, Sizable as _,
        table::{
            Column, DataTable, Table, TableBody, TableCaption, TableCell, TableDelegate, TableFooter, TableHead,
            TableHeader, TableRow, TableState,
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
    if param_bool(params, "story") {
        return story(params);
    }
    let size = size(params);
    let bordered = param_bool(params, "bordered");
    let stripe = param_bool(params, "stripe");
    let fixed = param_bool(params, "fixed_widths");
    let width = param_f32(params, "width", 360.);
    // `rows = 0`: the header over an empty body (Table has no empty view of its own).
    let count = (param_f32(params, "rows", ROWS.len() as f32) as usize).min(ROWS.len());
    // uikit-table: a footer row, a caption, a group heading row above the column heads.
    let footer = param_bool(params, "footer");
    let caption = param_bool(params, "caption");
    let group_header = param_bool(params, "group_header");
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
        let body = TableBody::new().children(ROWS.iter().take(count).enumerate().map(|(r, row)| {
            TableRow::new()
                .when(stripe && r % 2 == 1, |t| t.bg(even))
                .children((0..3).map(|i| cell(i, row[i])))
        }));
        let header = TableHeader::new()
            .when(group_header, |h| {
                h.child(
                    TableRow::new()
                        .child(TableHead::new().col_span(2).child("Invoice"))
                        .child(TableHead::new().text_right().child("Total")),
                )
            })
            .child(TableRow::new().children((0..3).map(head)));
        let table = Table::new()
            .with_size(size)
            .when(bordered, |t| t.border_1().border_color(border).rounded(radius))
            .child(header)
            .child(body)
            .when(footer, |t| {
                t.child(
                    TableFooter::new().child(
                        TableRow::new()
                            .child(TableCell::new().col_span(2).child("Total"))
                            .child(TableCell::new().text_right().child("$1,750.00")),
                    ),
                )
            })
            .when(caption, |t| t.child(TableCaption::new().child("A list of your recent invoices.")));
        div().w(px(width)).child(table).into_any_element()
    }))
}

// The story's invoices (table_story.rs), the statuses as text.
const INVOICES: [[&str; 5]; 7] = [
    ["INV001", "Paid", "Credit Card", "$250.00", "2024-01-15"],
    ["INV002", "Pending", "PayPal", "$150.00", "2024-02-01"],
    ["INV003", "Unpaid", "Bank Transfer", "$350.00", "2024-02-15"],
    ["INV004", "Paid", "Credit Card\nMaster Card / Visa", "$450.00", "2024-03-01"],
    ["INV005", "Paid", "PayPal", "$550.00", "2024-03-15"],
    ["INV006", "Pending", "Bank Transfer", "$200.00", "2024-04-01"],
    ["INV007", "Unpaid", "Credit Card", "$300.00", "2024-04-15"],
];

/// The story's Default section: Invoice w(150), Status over two columns,
/// Amount and Date right-aligned; the footer's Total over three columns and
/// the sum over two; the caption.
fn story(params: &Params) -> Result<Builder> {
    let size = size(params);
    let width = param_f32(params, "width", 560.);
    Ok(Rc::new(move |_, _, _| {
        let header = TableHeader::new().child(
            TableRow::new()
                .child(TableHead::new().w(px(150.)).child("Invoice"))
                .child(TableHead::new().col_span(2).child("Status"))
                .child(TableHead::new().text_right().child("Amount"))
                .child(TableHead::new().text_right().child("Date")),
        );
        let body = TableBody::new().children(INVOICES.iter().map(|[invoice, status, method, amount, date]| {
            TableRow::new()
                .child(TableCell::new().w(px(150.)).child(*invoice))
                .child(TableCell::new().child(*status))
                .child(TableCell::new().child(*method))
                .child(TableCell::new().text_right().child(*amount))
                .child(TableCell::new().text_right().child(*date))
        }));
        let footer = TableFooter::new().child(
            TableRow::new()
                .child(TableCell::new().col_span(3).child("Total"))
                .child(TableCell::new().col_span(2).text_right().child("$2,250.00")),
        );
        let table = Table::new()
            .with_size(size)
            .child(header)
            .child(body)
            .child(footer)
            .child(TableCaption::new().child("A list of your recent invoices."));
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
    rows: Vec<[&'static str; 3]>,
}

impl CaseDelegate {
    /// `sort`: "none" (no sortable column), "idle" (all sortable), "ascending" or
    /// "descending" (Amount sorted, the others sortable); the first `count` rows.
    fn new(sort: &str, widths: Option<Vec<f32>>, count: usize) -> Self {
        let columns = COLUMNS
            .iter()
            .enumerate()
            .map(|(ix, &(key, name, width, right))| {
                let width = widths.as_ref().and_then(|w| w.get(ix).copied()).unwrap_or(width);
                let column = Column::new(key, name).width(px(width));
                let column = if right { column.text_right() } else { column };
                match (sort, key) {
                    ("none", _) => column,
                    ("ascending", "amount") => column.ascending(),
                    ("descending", "amount") => column.descending(),
                    _ => column.sortable(),
                }
            })
            .collect();
        // The rows in the order the sorted column says, as an app's delegate sorts them.
        let mut rows = DATA.to_vec();
        match sort {
            "ascending" => rows.sort_by(|a, b| a[2].cmp(b[2])),
            "descending" => rows.sort_by(|a, b| b[2].cmp(a[2])),
            _ => {}
        }
        rows.truncate(count);
        Self { columns, rows }
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
        self.rows.len()
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
        div().w_full().flex().when(self.right(col_ix), |d| d.justify_end()).child(self.rows[row_ix][col_ix])
    }
}

pub fn data_table(params: &Params) -> Result<Builder> {
    let size = size(params);
    let stripe = param_bool(params, "stripe");
    let bordered = !param_bool(params, "borderless");
    let resizable = !param_bool(params, "fixed_columns");
    let width = param_f32(params, "width", 360.);
    let sort = param_str(params, "sort", "none").to_string();
    // Column widths other than COLUMNS' (a sortable header needs room for its sort box).
    let widths: Option<Vec<f32>> = params
        .get("widths")
        .and_then(|v| v.as_array())
        .map(|a| a.iter().filter_map(|w| w.as_f64()).map(|w| w as f32).collect());
    // Below the last row, so the rows do not fill the body (no filler rows, last rule kept).
    let extra = param_f32(params, "extra", 10.);
    // `rows = 0` shows the empty view (delegate.rs render_empty).
    let count = (param_f32(params, "rows", DATA.len() as f32) as usize).min(DATA.len());
    // TableSelection::Cell: a click selects a cell; `row_header` adds the 12px row header column.
    let cell_selectable = param_bool(params, "cell_selectable");
    let row_header = param_bool(params, "row_header");
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let state = cx.new(|cx| {
                TableState::new(CaseDelegate::new(&sort, widths.clone(), count), window, cx)
                    .col_selectable(false)
                    .col_movable(false)
                    .col_resizable(resizable)
                    .cell_selectable(cell_selectable)
                    .row_header(row_header)
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
        let height = chrome + size.table_row_height() * (count + 1) as f32 + px(extra);
        div()
            .w(px(width))
            .h(height)
            .child(DataTable::new(&state).with_size(size).stripe(stripe).bordered(bordered))
            .into_any_element()
    }))
}
