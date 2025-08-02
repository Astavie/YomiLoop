extends PopupPanel

@export var up_amount: int = 32

func _on_button_pressed():
    var pos = get_parent().global_position
    var rect = Rect2(pos + Vector2.UP * up_amount, Vector2.ZERO)
    popup(rect)

func _on_close_pressed():
    hide()
