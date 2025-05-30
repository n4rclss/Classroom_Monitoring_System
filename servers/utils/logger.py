import logging

def setup_logger(ip, port):
    logfile = f'app_{ip}_{port}.log'
    logger = logging.getLogger()
    logger.setLevel(logging.INFO)

    formatter = logging.Formatter('%(asctime)s - %(filename)s - %(funcName)s - line %(lineno)d - %(levelname)s - %(message)s')

    # File handler
    file_handler = logging.FileHandler(logfile)
    file_handler.setFormatter(formatter)

    # Console handler
    console_handler = logging.StreamHandler()
    console_handler.setFormatter(formatter)

    # Avoid duplicate logs if logger is already configured
    if not logger.handlers:
        logger.addHandler(file_handler)
        logger.addHandler(console_handler)
