import struct


class Sender:

    @staticmethod
    def SendString(message, conn, header_num_bytes, payloadLength_num_bytes):
        # Create header
        header = 'string'.ljust(header_num_bytes)        
        rawHeader = bytes(header, 'utf-8')	

        # Convert message to byte array
        rawPayload = bytes(message, 'utf-8')

        # Create payload length
        payloadLength = len(rawPayload)
        rawPayloadLength = payloadLength.to_bytes(
            payloadLength_num_bytes, byteorder="big"
        )

        # Compose the final packet: header + payload length + payload
        rawPacket = rawHeader + rawPayloadLength + rawPayload

        # Send bytes
        conn.sendall(rawPacket)

    def SendVector3(vector3, conn, header_num_bytes, payloadLength_num_bytes):
        # Create header
        header = 'vector3'.ljust(header_num_bytes)
        rawHeader = bytes(header, 'utf-8')

        # Convert vector3 tuple to byte array
        rawX = struct.pack('>f', vector3[0])
        rawY = struct.pack('>f', vector3[1])
        rawZ = struct.pack('>f', vector3[2])
        rawPayload = rawX + rawY + rawZ

        # Create payload length
        payloadLength = len(rawPayload)
        rawPayloadLength = payloadLength.to_bytes(
            payloadLength_num_bytes, byteorder="big"
        )

        # Compose the final packet: header + payload length + payload
        rawPacket = rawHeader + rawPayloadLength + rawPayload

        # Send bytes
        conn.sendall(rawPacket)

    def SendFloat(value, conn, header_num_bytes, payloadLength_num_bytes):
        # Create header
        header = 'float'.ljust(header_num_bytes)
        rawHeader = bytes(header, 'utf-8')

        # Convert float to byte array
        rawPayload = struct.pack('>f', value)

        # Create payload length
        payloadLength = len(rawPayload)
        rawPayloadLength = payloadLength.to_bytes(
            payloadLength_num_bytes, byteorder="big"
        )

        # Compose the final packet: header + payload length + payload
        rawPacket = rawHeader + rawPayloadLength + rawPayload

        # Send bytes
        conn.sendall(rawPacket)
