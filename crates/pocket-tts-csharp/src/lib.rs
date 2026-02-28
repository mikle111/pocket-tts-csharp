use candle_core::Device;
use num_enum::FromPrimitive;
use pocket_tts::{ModelState, TTSModel};
use std::collections::HashMap;
use std::ffi::CStr;
use std::os::raw::c_char;
use std::ptr;

use anyhow::Result;

pub type StreamChunkCallback =
    unsafe extern "C" fn(*mut AudioBuffer, *mut std::ffi::c_void) -> StreamControlCode;

pub type StreamFinishedCallback = unsafe extern "C" fn(*mut std::ffi::c_void);

pub type StreamErrorCallback = unsafe extern "C" fn(*mut std::ffi::c_void);

#[repr(C)]
pub struct AudioBuffer {
    data: *mut f32,
    length: usize,
}

#[repr(u8)]
#[derive(Debug, Clone, Copy, PartialEq, Eq, FromPrimitive)]
pub enum StreamControlCode {
    Proceed = 0,
    Stop = 1,
    #[num_enum(default)]
    Unknown = 2,
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_load_from_files(
    config_path: *const c_char,
    weights_path: *const c_char,
    tokenizer_path: *const c_char,
) -> *mut TTSModel {
    if config_path.is_null() || weights_path.is_null() || tokenizer_path.is_null() {
        return ptr::null_mut();
    }

    let config_path_str = unsafe {
        match CStr::from_ptr(config_path).to_str() {
            Ok(s) => s,
            Err(err) => {
                eprintln!("Bad config path: {:?}", err);
                return ptr::null_mut();
            }
        }
    };

    let weights_path_str = unsafe {
        match CStr::from_ptr(weights_path).to_str() {
            Ok(s) => s,
            Err(err) => {
                eprintln!("Bad weights path: {:?}", err);
                return ptr::null_mut();
            }
        }
    };

    let tokenizer_path_str = unsafe {
        match CStr::from_ptr(tokenizer_path).to_str() {
            Ok(s) => s,
            Err(err) => {
                eprintln!("Bad tokenizer path: {:?}", err);
                return ptr::null_mut();
            }
        }
    };

    // Read the files
    let config_bytes = match std::fs::read(config_path_str) {
        Ok(bytes) => bytes,
        Err(err) => {
            eprintln!("Can't read config: {:?}", err);
            return ptr::null_mut();
        }
    };

    let weights_bytes = match std::fs::read(weights_path_str) {
        Ok(bytes) => bytes,
        Err(err) => {
            eprintln!("Can't read weights: {:?}", err);
            return ptr::null_mut();
        }
    };

    let tokenizer_bytes = match std::fs::read(tokenizer_path_str) {
        Ok(bytes) => bytes,
        Err(err) => {
            eprintln!("Can't read tokenizer: {:?}", err);
            return ptr::null_mut();
        }
    };

    // Load model from bytes
    match TTSModel::load_from_bytes(&config_bytes, &weights_bytes, &tokenizer_bytes) {
        Ok(model) => Box::into_raw(Box::new(model)),
        Err(err) => {
            eprintln!("Failed to load model: {:?}", err);
            return ptr::null_mut();
        }
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_generate_stream(
    model: *mut TTSModel,
    text: *const c_char,
    voice: *mut ModelState,
    on_chunk: StreamChunkCallback,
    on_finished: StreamFinishedCallback,
    on_error: StreamErrorCallback,
    user_data: *mut std::ffi::c_void,
) {
    if model.is_null() || text.is_null() || voice.is_null() {
        unsafe {
            on_error(user_data);
        }
        return;
    }

    let model = unsafe { &*model };
    let voice = unsafe { &*voice };

    let text_str = unsafe {
        match CStr::from_ptr(text).to_str() {
            Ok(s) => s,
            Err(err) => {
                eprintln!("Can't get text: {:?}", err);
                on_error(user_data);
                return;
            }
        }
    };

    for chunk_result in model.generate_stream(text_str, voice) {
        let audio_tensor = match chunk_result {
            Ok(tensor) => tensor,
            Err(err) => {
                eprintln!("Failed to generate chunk: {:?}", err);
                unsafe {
                    on_error(user_data);
                }
                return;
            }
        };

        let audio_data = match audio_tensor.flatten_all().and_then(|t| t.to_vec1::<f32>()) {
            Ok(vec) => vec,
            Err(err) => {
                eprintln!("Error converting chunk tensor: {:?}", err);
                unsafe {
                    on_error(user_data);
                }
                return;
            }
        };

        let stream_control_code = StreamControlCode::from(unsafe {
            let audio_data_len = audio_data.len();
            let mut boxed_slice = audio_data.into_boxed_slice();
            let data_ptr = boxed_slice.as_mut_ptr();
            std::mem::forget(boxed_slice);
            let buffer = Box::into_raw(Box::new(AudioBuffer {
                data: data_ptr,
                length: audio_data_len,
            }));
            on_chunk(buffer, user_data)
        });

        match stream_control_code {
            StreamControlCode::Proceed => {}
            StreamControlCode::Stop => {
                unsafe {
                    on_finished(user_data);
                }
                return;
            }
            _ => {
                unsafe {
                    on_error(user_data);
                }
                return;
            }
        };
    }

    unsafe {
        on_finished(user_data);
    }
}

/// Generate speech from text using a voice
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_generate(
    model: *mut TTSModel,
    text: *const c_char,
    voice: *mut ModelState,
) -> *mut AudioBuffer {
    if model.is_null() || text.is_null() || voice.is_null() {
        return ptr::null_mut();
    }

    let model = unsafe { &*model };
    let voice = unsafe { &*voice };

    let text_str = unsafe {
        match CStr::from_ptr(text).to_str() {
            Ok(s) => s,
            Err(err) => {
                eprintln!("Can't get text: {:?}", err);
                return ptr::null_mut();
            }
        }
    };

    let audio_tensor = match model.generate(text_str, voice) {
        Ok(audio) => audio,
        Err(err) => {
            eprintln!("Failed to generate audio tensor {:?}", err);
            return ptr::null_mut();
        }
    };

    match audio_tensor.flatten_all().and_then(|t| t.to_vec1::<f32>()) {
        Ok(audio_data) => {
            let audio_data_len = audio_data.len();
            let mut boxed_slice = audio_data.into_boxed_slice();
            let data_ptr = boxed_slice.as_mut_ptr();
            std::mem::forget(boxed_slice);
            Box::into_raw(Box::new(AudioBuffer {
                data: data_ptr,
                length: audio_data_len,
            }))
        }
        Err(err) => {
            eprintln!("Error converting audio tensor to vector: {:?}", err);
            return ptr::null_mut();
        }
    }
}

/// Create a voice state from an audio file path
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_get_voice_state_from_wav(
    model: *mut TTSModel,
    path: *const c_char,
) -> *mut ModelState {
    if model.is_null() || path.is_null() {
        return ptr::null_mut();
    }

    let model = unsafe { &*model };
    let path_str = match unsafe { CStr::from_ptr(path) }.to_str() {
        Ok(s) => s,
        Err(err) => {
            eprintln!("Bad path: {:?}", err);
            return ptr::null_mut();
        }
    };

    match model.get_voice_state(path_str) {
        Ok(state) => Box::into_raw(Box::new(state)),
        Err(err) => {
            eprintln!("Error getting voice state: {:?}", err);
            return ptr::null_mut();
        }
    }
}

/// Create a voice state from an safetensors file path
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_get_voice_state_from_safetensors(
    model: *mut TTSModel,
    path: *const c_char,
) -> *mut ModelState {
    if model.is_null() || path.is_null() {
        return ptr::null_mut();
    }

    let model = unsafe { &*model };
    let path_str = match unsafe { CStr::from_ptr(path) }.to_str() {
        Ok(s) => s,
        Err(err) => {
            eprintln!("Bad path: {:?}", err);
            return ptr::null_mut();
        }
    };

    match model.get_voice_state_from_prompt_file(path_str) {
        Ok(state) => Box::into_raw(Box::new(state)),
        Err(err) => {
            eprintln!("Failed to get voice from safetensors: {:?}", err);
            return ptr::null_mut();
        }
    }
}

/// Create a voice saftensors from wav
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_create_safetensors_from_wav(
    model: *mut TTSModel,
    wav_path: *const c_char,
    safetensors_path: *const c_char,
) {
    if model.is_null() || wav_path.is_null() || safetensors_path.is_null() {
        eprintln!("Null in parameters");
        return;
    }

    let model = unsafe { &*model };
    let wav_path_str = match unsafe { CStr::from_ptr(wav_path) }.to_str() {
        Ok(s) => s,
        Err(err) => {
            eprintln!("Bad wav_path: {:?}", err);
            return;
        }
    };
    let safetensors_path_str = match unsafe { CStr::from_ptr(safetensors_path) }.to_str() {
        Ok(s) => s,
        Err(err) => {
            eprintln!("Bad safetensors_path: {:?}", err);
            return;
        }
    };

    match model.save_audio_as_voice_prompt(wav_path_str, safetensors_path_str) {
        Ok(_) => {}
        Err(err) => eprintln!("Failed to save safetensors: {:?}", err),
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_copy_voice_state(state: *mut ModelState) -> *mut ModelState {
    if state.is_null() {
        return std::ptr::null_mut();
    }

    let state = unsafe { &*state };

    match deep_clone_voice_state(state, &Device::Cpu) {
        Ok(cloned_state) => Box::into_raw(Box::new(cloned_state)),
        Err(err) => {
            eprintln!("Failed to clone state to CPU: {:?}", err);
            std::ptr::null_mut()
        }
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_free_voice_state(state: *mut ModelState) {
    if !state.is_null() {
        unsafe {
            let _ = Box::from_raw(state);
        }
    }
}

/// Get the sample rate of the model
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_sample_rate(model: *const TTSModel) -> u32 {
    if model.is_null() {
        return 0;
    }
    let model = unsafe { &*model };
    model.sample_rate as u32
}

/// Free an audio buffer
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_free_audio(buffer: *mut AudioBuffer) {
    if !buffer.is_null() {
        unsafe {
            let buffer = Box::from_raw(buffer);
            if !buffer.data.is_null() {
                let _ = Vec::from_raw_parts(buffer.data, buffer.length, buffer.length);
            }
        }
    }
}

/// Free TTS model
#[unsafe(no_mangle)]
pub unsafe extern "C" fn pocket_tts_free(model: *mut TTSModel) {
    if !model.is_null() {
        unsafe {
            let _ = Box::from_raw(model);
        }
    }
}

fn deep_clone_voice_state(state: &ModelState, device: &Device) -> Result<ModelState> {
    let mut new_state = HashMap::new();

    for (module_name, params) in state {
        let mut new_params = HashMap::new();
        for (param_name, tensor) in params {
            let cloned_tensor = tensor.to_device(device)?.copy()?;
            new_params.insert(param_name.clone(), cloned_tensor);
        }
        new_state.insert(module_name.clone(), new_params);
    }

    Ok(new_state)
}
