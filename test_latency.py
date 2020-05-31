from tornado.websocket import websocket_connect
import asyncio
from datetime import datetime
import os
import random
import string



def log(name, message):
    print('%s: %s' % (name, message))


async def single_test(address, name):
    try:
        ws = await websocket_connect(address)
    except:
        log(name, "Error connecting to %s" % address)
        return -1

    amount = 1000
    string_size = 1500
    message = ''.join(random.choice(string.ascii_uppercase + string.digits) for _ in range(string_size))

    try:
        for i in range(amount):
            start_time = datetime.now()
            await ws.write_message(message)
            received = await ws.read_message()
            end_time = datetime.now()

            if message != received:
                log('Loopback error')
                return

            time = (datetime.now() - start_time).total_seconds() * 1000
            log(name, "%d completed - time: %1.0fms" % (i, time))
    except:
        log(name, "Error")


async def run_test():
    #host = '127.0.0.1'
    host = 'mapping_engine'
    port = 9002  # int(sys.argv[5])
    websocket_address = "ws://%s:%d/latency/" % (host, port)

    res = []
    parallel_runs = 1
    for i in range(parallel_runs):
        res += [single_test(websocket_address, '%d' % i)]

    return await asyncio.gather(*res)


if __name__ == '__main__':
    start_time = datetime.now()
    loop = asyncio.get_event_loop()
    loop.run_until_complete(run_test())
    time = (datetime.now() - start_time).total_seconds()
    log("Session completed - total time in Seconds", time)
    loop.close()
