from Crypto.Cipher import AES
from Crypto.Util.Padding import pad, unpad
from Crypto.Random import get_random_bytes
import base64

class AESManager:
    def __init__(self):
        self.key = b'ThisIsASecretKey'

    def encrypt(self, plaintext: str) -> str:
        # Generate a random 16-byte IV
        iv = get_random_bytes(16)
        cipher = AES.new(self.key, AES.MODE_CBC, iv)
        ciphertext = cipher.encrypt(pad(plaintext.encode('utf-8'), AES.block_size))
        # Return IV + ciphertext, base64 encoded for safe transmission
        return base64.b64encode(iv + ciphertext).decode()

    def decrypt(self, b64_ciphertext: str) -> str:
        data = base64.b64decode(b64_ciphertext)
        iv = data[:16]
        ciphertext = data[16:]
        cipher = AES.new(self.key, AES.MODE_CBC, iv)
        plaintext = unpad(cipher.decrypt(ciphertext), AES.block_size)
        return plaintext.decode()
