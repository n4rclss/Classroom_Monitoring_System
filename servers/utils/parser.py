from servers.models.packets import *
import json

MESSAGE_TYPES = {
    "login": PacketLogin
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
