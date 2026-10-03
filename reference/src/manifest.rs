//! Case specifications (`cases/*.toml`) and their expansion into concrete cases.
//!
//! A spec lists groups of parameter combinations. Each combination is rendered
//! in every listed state and theme. The expanded list is written to
//! `manifest.json`, which is the only file the Avalonia tests read.
use anyhow::{Context as _, Result, bail};
use serde::Deserialize;
use serde_json::{Map, Value};
use std::path::Path;

pub type Params = Map<String, Value>;

#[derive(Debug, Deserialize)]
pub struct Spec {
    pub component: String,
    pub viewport: [f32; 2],
    pub anchor: [f32; 2],
    #[serde(default = "default_themes")]
    pub themes: Vec<String>,
    #[serde(default)]
    pub group: Vec<Group>,
    #[serde(default)]
    pub motion: Vec<Motion>,
}

fn default_themes() -> Vec<String> {
    vec!["light".into(), "dark".into()]
}

#[derive(Debug, Deserialize)]
pub struct Group {
    #[serde(default)]
    pub name: Option<String>,
    #[serde(default)]
    pub params: toml::Table,
    #[serde(default)]
    pub matrix: toml::Table,
    #[serde(default = "default_states")]
    pub states: Vec<String>,
    #[serde(default)]
    pub viewport: Option<[f32; 2]>,
    #[serde(default)]
    pub anchor: Option<[f32; 2]>,
    #[serde(default)]
    pub themes: Option<Vec<String>>,
}

fn default_states() -> Vec<String> {
    vec!["normal".into()]
}

/// A state change recorded frame by frame on the executor clock.
#[derive(Debug, Deserialize)]
pub struct Motion {
    pub name: String,
    #[serde(default)]
    pub params: toml::Table,
    /// The interaction that starts the motion, e.g. "click" or "hover".
    pub trigger: String,
    /// The state the component is in before the trigger.
    #[serde(default = "default_from_state")]
    pub from: String,
    /// Milliseconds after the trigger at which frames are captured.
    pub samples_ms: Vec<u64>,
    #[serde(default)]
    pub themes: Option<Vec<String>>,
    #[serde(default)]
    pub viewport: Option<[f32; 2]>,
    #[serde(default)]
    pub anchor: Option<[f32; 2]>,
}

fn default_from_state() -> String {
    "normal".into()
}

#[derive(Debug, Clone)]
pub struct Case {
    pub id: String,
    pub component: String,
    pub group: Option<String>,
    pub params: Params,
    pub state: String,
    pub theme: String,
    pub viewport: [f32; 2],
    pub anchor: [f32; 2],
    pub motion: Option<MotionSample>,
}

#[derive(Debug, Clone)]
pub struct MotionSample {
    pub name: String,
    pub trigger: String,
    pub from: String,
    pub samples_ms: Vec<u64>,
}

pub fn load_specs(dir: &Path) -> Result<Vec<Spec>> {
    let mut paths: Vec<_> = std::fs::read_dir(dir)?
        .filter_map(|entry| entry.ok().map(|entry| entry.path()))
        .filter(|path| path.extension().is_some_and(|ext| ext == "toml"))
        .collect();
    paths.sort();
    paths
        .iter()
        .map(|path| {
            let text = std::fs::read_to_string(path)?;
            toml::from_str(&text).with_context(|| format!("parsing {}", path.display()))
        })
        .collect()
}

fn to_json(value: &toml::Value) -> Value {
    serde_json::to_value(value).expect("TOML values convert to JSON")
}

/// The id fragment for one parameter value: strings as-is, `true` as the key,
/// numbers as `key-value`; `false` is omitted.
fn fragment(key: &str, value: &Value) -> Option<String> {
    match value {
        Value::String(s) => Some(s.clone()),
        Value::Bool(true) => Some(key.to_string()),
        Value::Bool(false) => None,
        Value::Number(n) => Some(format!("{key}-{n}")),
        other => Some(format!("{key}-{other}")),
    }
}

fn product(matrix: &toml::Table) -> Result<Vec<Vec<(String, Value)>>> {
    let mut combos: Vec<Vec<(String, Value)>> = vec![vec![]];
    for (key, values) in matrix {
        let values = values
            .as_array()
            .with_context(|| format!("matrix entry {key} must be an array"))?;
        let mut next = Vec::new();
        for combo in &combos {
            for value in values {
                let mut combo = combo.clone();
                combo.push((key.clone(), to_json(value)));
                next.push(combo);
            }
        }
        combos = next;
    }
    Ok(combos)
}

pub fn expand(spec: &Spec) -> Result<Vec<Case>> {
    let mut cases = Vec::new();
    for group in &spec.group {
        let themes = group.themes.clone().unwrap_or_else(|| spec.themes.clone());
        for combo in product(&group.matrix)? {
            let mut params: Params = group
                .params
                .iter()
                .map(|(k, v)| (k.clone(), to_json(v)))
                .collect();
            let mut parts: Vec<String> = Vec::new();
            for (key, value) in &combo {
                if let Some(part) = fragment(key, value) {
                    parts.push(part);
                }
                params.insert(key.clone(), value.clone());
            }
            let mut variant = parts.join(".");
            if variant.is_empty() {
                variant = "base".into();
            }
            if let Some(name) = &group.name {
                variant = format!("{name}.{variant}");
            }
            for state in &group.states {
                for theme in &themes {
                    cases.push(Case {
                        id: format!("{}/{variant}/{state}/{theme}", spec.component),
                        component: spec.component.clone(),
                        group: group.name.clone(),
                        params: params.clone(),
                        state: state.clone(),
                        theme: theme.clone(),
                        viewport: group.viewport.unwrap_or(spec.viewport),
                        anchor: group.anchor.unwrap_or(spec.anchor),
                        motion: None,
                    });
                }
            }
        }
    }
    for motion in &spec.motion {
        let themes = motion.themes.clone().unwrap_or_else(|| spec.themes.clone());
        let params: Params = motion
            .params
            .iter()
            .map(|(k, v)| (k.clone(), to_json(v)))
            .collect();
        for theme in &themes {
            cases.push(Case {
                id: format!("{}/motion.{}/{}/{theme}", spec.component, motion.name, motion.trigger),
                component: spec.component.clone(),
                group: Some(format!("motion.{}", motion.name)),
                params: params.clone(),
                state: motion.from.clone(),
                theme: theme.clone(),
                viewport: motion.viewport.unwrap_or(spec.viewport),
                anchor: motion.anchor.unwrap_or(spec.anchor),
                motion: Some(MotionSample {
                    name: motion.name.clone(),
                    trigger: motion.trigger.clone(),
                    from: motion.from.clone(),
                    samples_ms: motion.samples_ms.clone(),
                }),
            });
        }
    }
    let mut seen = std::collections::HashSet::new();
    for case in &cases {
        if !seen.insert(case.id.clone()) {
            bail!("duplicate case id {}", case.id);
        }
    }
    Ok(cases)
}

pub fn param_str<'a>(params: &'a Params, key: &str, default: &'a str) -> &'a str {
    params.get(key).and_then(Value::as_str).unwrap_or(default)
}

pub fn param_bool(params: &Params, key: &str) -> bool {
    params.get(key).and_then(Value::as_bool).unwrap_or(false)
}

pub fn param_f32(params: &Params, key: &str, default: f32) -> f32 {
    params
        .get(key)
        .and_then(Value::as_f64)
        .map(|v| v as f32)
        .unwrap_or(default)
}
