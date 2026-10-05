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
        date_picker::{DatePicker, DatePickerState, DateRangePreset},
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
    let end = date(params, "end", "none");
    let range = param_bool(params, "range");
    // The month shown without a selection (today's month when unset).
    let display = date(params, "month", "none");
    let today = date(params, "today", "2025-06-10").expect("today");
    let view_kind = param_str(params, "view", "day").to_string();
    let blackout = (date(params, "blackout_from", "none"), date(params, "blackout_to", "none"));
    let weekdays = weekdays(params);
    let months = param_f32(params, "months", 1.) as usize;
    let first_day = if param_str(params, "first_day", "sun") == "mon" { Weekday::Mon } else { Weekday::Sun };
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            gpui_base::test_clock::set_today(Some(today));
            let view_kind = view_kind.clone();
            let weekdays = weekdays.clone();
            let state = cx.new(|cx| {
                let mut s = CalendarState::new(window, cx);
                if let (Some(a), Some(b)) = blackout {
                    s = s.disabled_matcher(Matcher::range(Some(a), Some(b)));
                }
                if !weekdays.is_empty() {
                    s = s.disabled_matcher(Matcher::DayOfWeek(weekdays));
                }
                // With no selection the calendar shows the month of the date the case names.
                s.apply_date(Date::Single(selected.or(display).or(Some(today))));
                if range {
                    s.apply_date(Date::Range(selected, end));
                } else if selected.is_none() {
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
        Calendar::new(&state)
            .with_size(size)
            .number_of_months(months)
            .first_day_of_week(first_day)
            .into_any_element()
    }))
}

/// Matcher::DayOfWeek numbers ("0,6": Sunday and Saturday), none when unset.
fn weekdays(params: &Params) -> Vec<u32> {
    param_str(params, "disabled_weekdays", "")
        .split(',')
        .filter_map(|n| n.trim().parse().ok())
        .collect()
}

/// The presets a case shows: the labels of GPUI's story, on dates fixed
/// before the cases' March 2025.
fn presets(range: bool) -> Vec<DateRangePreset> {
    let day = |m, d| NaiveDate::from_ymd_opt(2025, m, d).expect("date");
    if range {
        vec![
            DateRangePreset::range("Last 7 Days", day(3, 7), day(3, 14)),
            DateRangePreset::range("Last 14 Days", day(2, 28), day(3, 14)),
            DateRangePreset::range("Last 30 Days", day(2, 12), day(3, 14)),
        ]
    } else {
        vec![
            DateRangePreset::single("Yesterday", day(3, 13)),
            DateRangePreset::single("Last Week", day(3, 7)),
            DateRangePreset::single("Last Month", day(2, 12)),
        ]
    }
}

/// `DatePicker` (crates/component/src/time/date_picker.rs): the field, and its
/// calendar popup once opened.
pub fn date_picker(params: &Params) -> Result<Builder> {
    let size = size(params);
    let disabled = disabled(params);
    let selected = date(params, "date", "none");
    let end = date(params, "end", "none");
    let range = param_bool(params, "range");
    let today = date(params, "today", "2025-06-10").expect("today");
    let width = param_f32(params, "width", 220.);
    let months = param_f32(params, "months", 1.) as usize;
    let cleanable = param_bool(params, "cleanable");
    let with_presets = param_bool(params, "presets");
    let appearance = !param_bool(params, "plain");
    let weekdays = weekdays(params);
    let placeholder = params.get("placeholder").and_then(|v| v.as_str()).map(str::to_string);
    let precision = match param_str(params, "precision", "none") {
        "minute" => Some(TimePrecision::Minute),
        "second" => Some(TimePrecision::Second),
        _ => None,
    };
    let cycle = match param_str(params, "cycle", "h23") {
        "h12" => HourCycle::H12,
        _ => HourCycle::H23,
    };
    let time = NaiveTime::parse_from_str(param_str(params, "time", "00:00:00"), "%H:%M:%S")?;
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            gpui_base::test_clock::set_today(Some(today));
            let weekdays = weekdays.clone();
            let state = cx.new(|cx| {
                let mut s = if range { DatePickerState::range(window, cx) } else { DatePickerState::new(window, cx) };
                if let Some(precision) = precision {
                    s = s.time_precision(precision).hour_cycle(cycle);
                }
                if !weekdays.is_empty() {
                    s = s.disabled_matcher(Matcher::DayOfWeek(weekdays));
                }
                match (range, selected, end) {
                    (true, Some(a), Some(b)) => s.set_date((a, b), window, cx),
                    (false, Some(d), _) if precision.is_some() => s.set_date_time(d.and_time(time), window, cx),
                    (false, Some(d), _) => s.set_date(d, window, cx),
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
            .and_then(|e| e.downcast::<DatePickerState>().ok())
            .expect("date picker state");
        let mut picker = DatePicker::new(&state)
            .with_size(size)
            .disabled(disabled)
            .number_of_months(months)
            .cleanable(cleanable)
            .appearance(appearance)
            .w(px(width));
        if let Some(placeholder) = &placeholder {
            picker = picker.placeholder(placeholder.clone());
        }
        if with_presets {
            picker = picker.presets(presets(range));
        }
        picker.into_any_element()
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

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    // calendar.rs: weekday titles and disabled days are muted at half opacity.
    out.push(("calendar.weekday".into(), theme.muted_foreground.opacity(0.5).into()));
    out.push(("calendar.disabled".into(), theme.muted_foreground.opacity(0.5).into()));
}
