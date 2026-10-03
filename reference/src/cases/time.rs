//! `Calendar` (crates/component/src/time/calendar.rs) over a `CalendarState`
//! whose "today" the case pins (reference patch: test_clock::set_today).
use super::size;
use crate::{
    harness::Builder,
    manifest::{Params, param_str},
};
use anyhow::Result;
use chrono::{NaiveDate, Weekday};
use gpui_kit::{
    AppContext as _, IntoElement as _,
    component::{
        Sizable as _,
        calendar::{Calendar, CalendarState, Date, Matcher},
    },
};
use std::rc::Rc;

/// A "YYYY-MM-DD" parameter; "none" (or anything unparsable) is no date.
pub fn date(params: &Params, key: &str, default: &str) -> Option<NaiveDate> {
    NaiveDate::parse_from_str(param_str(params, key, default), "%Y-%m-%d").ok()
}

pub fn calendar(params: &Params) -> Result<Builder> {
    let size = size(params);
    let selected = date(params, "date", "2025-03-14");
    let today = date(params, "today", "2025-06-10").expect("today");
    let view_kind = param_str(params, "view", "day").to_string();
    let blackout = (date(params, "blackout_from", "none"), date(params, "blackout_to", "none"));
    let first_day = if param_str(params, "first_day", "sun") == "mon" { Weekday::Mon } else { Weekday::Sun };
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            gpui_base::test_clock::set_today(Some(today));
            let view_kind = view_kind.clone();
            let state = cx.new(|cx| {
                let mut s = CalendarState::new(window, cx);
                if let (Some(a), Some(b)) = blackout {
                    s = s.disabled_matcher(Matcher::range(Some(a), Some(b)));
                }
                // With no selection the calendar shows the month of the date the case names.
                s.apply_date(Date::Single(selected.or(Some(today))));
                if selected.is_none() {
                    s.apply_date(Date::Single(None));
                }
                match view_kind.as_str() {
                    "month" => s.set_view(gpui_base::CalendarView::Month),
                    "year" => s.set_view(gpui_base::CalendarView::Year),
                    _ => {}
                }
                s
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<CalendarState>().ok())
            .expect("calendar state");
        Calendar::new(&state).with_size(size).first_day_of_week(first_day).into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // calendar.rs: weekday titles and disabled days are muted at half opacity.
    out.push(("calendar.weekday".into(), theme.muted_foreground.opacity(0.5)));
    out.push(("calendar.disabled".into(), theme.muted_foreground.opacity(0.5)));
}
