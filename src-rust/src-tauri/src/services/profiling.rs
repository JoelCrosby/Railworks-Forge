use std::time::Instant;

pub struct ProfileSpan {
    name: &'static str,
    start: Instant,
}

impl ProfileSpan {
    pub fn new(name: &'static str) -> Self {
        tracing::debug!(target: "railworks_forge::profile", operation = name, "started");
        Self {
            name,
            start: Instant::now(),
        }
    }
}

impl Drop for ProfileSpan {
    fn drop(&mut self) {
        let elapsed = self.start.elapsed();
        tracing::info!(
            target: "railworks_forge::profile",
            operation = self.name,
            elapsed_ms = elapsed.as_millis(),
            "completed"
        );
    }
}
