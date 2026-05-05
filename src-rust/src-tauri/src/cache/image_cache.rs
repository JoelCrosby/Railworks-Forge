use lru::LruCache;
use std::{
    num::NonZeroUsize,
    sync::{Arc, Mutex},
};

const CAPACITY: usize = 256;

/// Thread-safe LRU image cache keyed by a composite path string.
/// Stores raw image bytes; the frontend handles decoding.
#[derive(Clone)]
pub struct ImageCache(Arc<Mutex<LruCache<String, Vec<u8>>>>);

impl ImageCache {
    pub fn new() -> Self {
        Self(Arc::new(Mutex::new(LruCache::new(
            NonZeroUsize::new(CAPACITY).unwrap(),
        ))))
    }

    pub fn get(&self, key: &str) -> Option<Vec<u8>> {
        self.0.lock().unwrap().get(key).cloned()
    }

    pub fn insert(&self, key: String, bytes: Vec<u8>) {
        self.0.lock().unwrap().put(key, bytes);
    }

    pub fn cache_key(archive_path: &str, entry_name: &str) -> String {
        format!("{archive_path}::{entry_name}")
    }
}

impl Default for ImageCache {
    fn default() -> Self {
        Self::new()
    }
}
