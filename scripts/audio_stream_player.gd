@tool
extends AudioStreamPlayer

@export var volume : float : set = set_volume
@export_range(20.0, 20000.0, 2) var filter_cutoff : float : set = setter_func

func setter_func(value):
    filter_cutoff = value
    AudioServer.get_bus_effect(1, 0).cutoff_hz = value

func set_volume(value):
    volume = value
    AudioServer.set_bus_volume_linear(1, value)
