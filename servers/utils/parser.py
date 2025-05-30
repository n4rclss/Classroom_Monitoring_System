from models.messages import LoginMessage, JoinClassMessage, ChatMessage
import json

MESSAGE_TYPES = {
    "login": LoginMessage,
    "join_class": JoinClassMessage,
    "chat_message": ChatMessage
}

def parse_message(raw_data: bytes):
    from json import JSONDecodeError
    from pydantic import ValidationError
    
    try:
        json_data = raw_data.decode().strip()
        message_type = json.loads(json_data).get('type')
        
        if not message_type or message_type not in MESSAGE_TYPES:
            raise ValueError("Invalid message type")
            
        model = MESSAGE_TYPES[message_type]
        return model.model_validate_json(json_data)
        
    except JSONDecodeError:
        raise ValidationError("Invalid JSON format")
    except KeyError:
        raise ValidationError("Missing required fields")
