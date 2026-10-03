//! `Calendar` (crates/component/src/time/calendar.rs) over a `CalendarState`
//! whose "today" the case pins (reference patch: test_clock::set_today).
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use chrono::{NaiveDate, NaiveTime, Weekday};
use gpui_kit::{
    AppContext as _, IntoElement as _, Styled as _, px,
    component::{
        Disableable as _, Sizable as _,
        calendar::{Calendar, CalendarState, Date, Matcher},
        date_picker::{DatePicker, DatePickerState},
        time_field::{HourCycle, TimeField, TimeFieldState, TimePrecision},
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

/// `DatePicker` (crates/component/src/time/date_picker.rs): the field, and its
/// calendar popup once opened.
pub fn date_picker(params: &Params) -> Result<Builder> {
    let size = size(params);
    let disabled = disabled(params);
    let selected = date(params, "date", "none");
    let today = date(params, "today", "2025-06-10").expect("today");
    let width = param_f32(params, "width", 220.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            gpui_base::test_clock::set_today(Some(today));
            let state = cx.new(|cx| {
                let mut s = DatePickerState::new(window, cx);
                if let Some(d) = selected {
                    s.set_date(d, window, cx);
                }
                s
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<DatePickerState>().ok())
            .expect("date picker state");
        DatePicker::new(&state).with_size(size).disabled(disabled).w(px(width)).into_any_element()
    }))
}

/// `TimeField` (crates/component/src/time/time_field.rs): the segmented field.
pub fn time_field(params: &Params) -> Result<Builder> {
    let size = size(params);
    let disabled = disabled(params);
    let invalid = param_bool(params, "invalid");
    let time = NaiveTime::parse_from_str(param_str(params, "time", "09:30:15"), "%H:%M:%S")?;
    let precision = match param_str(params, "precision", "minute") {
        "second" => TimePrecision::Second,
        _ => TimePrecision::Minute,
    };
    let cycle = match param_str(params, "cycle", "h23") {
        "h12" => HourCycle::H12,
        _ => HourCycle::H23,
    };
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let state = cx.new(|cx| {
                let mut s = TimeFieldState::new(window, cx).precision(precision).hour_cycle(cycle);
                s.set_time(time, window, cx);
                s
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<TimeFieldState>().ok())
            .expect("time field state");
        TimeField::new(&state).with_size(size).disabled(disabled).invalid(invalid).into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // calendar.rs: weekday titles and disabled days are muted at half opacity.
    out.push(("calendar.weekday".into(), theme.muted_foreground.opacity(0.5)));
    out.push(("calendar.disabled".into(), theme.muted_foreground.opacity(0.5)));
}
