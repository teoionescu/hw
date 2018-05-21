
package common;

public class HandshakeMessage implements Message
{
    public String name;
    
    public HandshakeMessage() { }

    @Override
    public MsgType getType() {
        return MsgType.MSG_HANDSHAKE;
    }

    @Override
    public String stringFormat() {
        return "Name set: " + this.name;
    }
}